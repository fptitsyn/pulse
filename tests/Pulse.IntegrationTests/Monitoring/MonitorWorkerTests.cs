using System.Net;
using System.Net.Http.Json;
using app.Models;
using app.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Monitoring;

public class MonitorWorkerTests(PulseWebApplicationFactory<Program> factory) : IClassFixture<PulseWebApplicationFactory<Program>>
{
    [Fact]
    public async Task MonitorProcessor_WithOverdueMonitor_SavesExactlyOneResult()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, httpClient);

        var monitorId = await TestHelper.CreateMonitorAndGetIdAsync(factory);
        await TestHelper.MakeMonitorDueAsync(dbContext, monitorId, true, TestContext.Current.CancellationToken);
        
        // Act
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var checkResults = await verificationDb.CheckResults.AsNoTracking()
            .Where(r => r.MonitorId == monitorId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var checkResult = Assert.Single(checkResults);
        Assert.Equal(CheckOutcomeType.Success, checkResult.Outcome);

        var updatedMonitor = await verificationDb.Monitors.AsNoTracking()
            .SingleAsync(m => m.MonitorId == monitorId, TestContext.Current.CancellationToken);

        Assert.Equal(checkResult.CheckedAt.AddSeconds(TestHelper.NewMonitor().IntervalSeconds), updatedMonitor.NextCheckAt);
    }

    [Fact]
    public async Task DisabledMonitor_DoesNotExecute()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, httpClient);
        
        var monitorId = await TestHelper.CreateMonitorAndGetIdAsync(factory);
        await TestHelper.MakeMonitorDueAsync(dbContext, monitorId, false, TestContext.Current.CancellationToken);
        
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var checkResults = await verificationDb.CheckResults.AsNoTracking()
            .Where(r => r.MonitorId == monitorId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(checkResults);

        var updatedMonitor = await verificationDb.Monitors.AsNoTracking()
            .SingleAsync(m => m.MonitorId == monitorId, TestContext.Current.CancellationToken);

        Assert.False(updatedMonitor.IsEnabled);
    }
    
    [Fact]
    public async Task NotDueMonitor_DoesNotExecute()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, httpClient);
        
        var monitorId = await TestHelper.CreateMonitorAndGetIdAsync(factory);
        var monitor = await dbContext.Monitors.FindAsync([monitorId], TestContext.Current.CancellationToken);
        Assert.NotNull(monitor);

        monitor.IsEnabled = true;
        monitor.NextCheckAt = DateTime.UtcNow.AddMinutes(5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var checkResults = await verificationDb.CheckResults.AsNoTracking()
            .Where(r => r.MonitorId == monitorId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(checkResults);
    }

    [Fact]
    public async Task InvalidMonitor_DoesNotPrevent_FurtherExecution()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, httpClient);

        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);
        
        var invalidService = new ServiceDto
        {
            BaseUrl = "https://localhost",
            Name = "InternalTest"
        };
        
        var invalidMonitorDto = new MonitorDto
        {
            Path = "health",
            HttpMethod = "Get",
            IntervalSeconds = 5,
            IsEnabled = true
        };
        
        using var servicesResponse = await client.PostAsJsonAsync("/services", invalidService,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, servicesResponse.StatusCode);
        var serviceLocation = servicesResponse.Headers.Location;
        
        using var monitorResponse = await client.PostAsJsonAsync($"{serviceLocation}/monitors", invalidMonitorDto,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, monitorResponse.StatusCode);
        
        var invalidMonitorLocation = monitorResponse.Headers.Location;
        var invalidMonitorId = int.Parse(invalidMonitorLocation!.ToString().TrimEnd('/').Split('/')[^1]);
        var validMonitorLocation = await TestHelper.CreateMonitorAsync(client);
        var validMonitorId = int.Parse(validMonitorLocation!.ToString().TrimEnd('/').Split('/')[^1]);

        await TestHelper.MakeMonitorDueAsync(dbContext, invalidMonitorId, true, TestContext.Current.CancellationToken);
        await TestHelper.MakeMonitorDueAsync(dbContext, validMonitorId, true, TestContext.Current.CancellationToken);
        
        var invalidMonitor = await dbContext.Monitors.Include(m => m.Service)
            .SingleAsync(m => m.MonitorId == invalidMonitorId, TestContext.Current.CancellationToken);

        invalidMonitor.Service.BaseUrl = "not-a-valid-url";

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var checkResults = await verificationDb.CheckResults.AsNoTracking()
            .Where(r => r.MonitorId == validMonitorId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var checkResult = Assert.Single(checkResults);

        var validMonitor = await verificationDb.Monitors.AsNoTracking()
            .SingleAsync(m => m.MonitorId == validMonitorId, TestContext.Current.CancellationToken);

        Assert.Equal(checkResult.CheckedAt.AddSeconds(TestHelper.NewMonitor().IntervalSeconds), validMonitor.NextCheckAt);
        Assert.True(validMonitor.IsEnabled);
    }
    
    [Theory]
    [InlineData(typeof(SuccessHttpHandler), ServiceStatus.Up)]
    [InlineData(typeof(ServiceUnavailableHandler), ServiceStatus.Down)]
    [InlineData(typeof(TimeoutHttpHandler), ServiceStatus.Down)]
    [InlineData(typeof(NetworkFailureHttpHandler), ServiceStatus.Down)]
    [InlineData(typeof(NotFoundHandler), ServiceStatus.Down)]
    public async Task MonitorOutcome_SetsExpectedServiceStatus(Type handlerType, ServiceStatus expectedStatus)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = (HttpMessageHandler)Activator.CreateInstance(handlerType)!;
        using var httpClient = new HttpClient(handler);
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, httpClient);
    
        int monitorId = await TestHelper.CreateMonitorAndGetIdAsync(factory);
        await TestHelper.MakeMonitorDueAsync(dbContext, monitorId, true, TestContext.Current.CancellationToken);
        var monitor = await dbContext.Monitors.FindAsync([monitorId], TestContext.Current.CancellationToken);
    
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        var service = await dbContext.Services.FindAsync([monitor.ServiceId], TestContext.Current.CancellationToken);
        var serviceStatusHandler = scope.ServiceProvider.GetRequiredService<ServiceStatusHandler>();
        await serviceStatusHandler.ConsiderServiceStatus(service, TestContext.Current.CancellationToken);
        
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var persistedService = await verificationDb.Services.AsNoTracking()
            .SingleAsync(s => s.ServiceId == monitor.ServiceId, TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, persistedService.ServiceStatus);
    }
}