using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
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
    private HttpMessageHandler _successHandler = new SuccessHttpHandler();
    private HttpMessageHandler _timeoutHandler = new TimeoutHttpHandler();
    
    [Fact]
    public async Task MonitorProcessor_WithOverdueMonitor_SavesExactlyOneResult()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = CreateProcessor(scope.ServiceProvider, dbContext, httpClient);

        var monitorId = await CreateMonitorAndGetIdAsync(factory);
        await MakeMonitorDueAsync(dbContext, monitorId, true, TestContext.Current.CancellationToken);
        
        // Act
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var checkResults = await verificationDb.CheckResults.AsNoTracking()
            .Where(r => r.MonitorId == monitorId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var checkResult = Assert.Single(checkResults);
        Assert.Equal(HttpMonitorChecker.Success, checkResult.Outcome);

        var updatedMonitor = await verificationDb.Monitors.AsNoTracking()
            .SingleAsync(m => m.MonitorId == monitorId, TestContext.Current.CancellationToken);

        Assert.Equal(checkResult.CheckedAt.AddSeconds(TestData.TestMonitor.IntervalSeconds), updatedMonitor.NextCheckAt);
    }

    [Fact]
    public async Task DisabledMonitor_DoesNotExecute()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = CreateProcessor(scope.ServiceProvider, dbContext, httpClient);
        
        var monitorId = await CreateMonitorAndGetIdAsync(factory);
        await MakeMonitorDueAsync(dbContext, monitorId, false, TestContext.Current.CancellationToken);
        
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
        var processor = CreateProcessor(scope.ServiceProvider, dbContext, httpClient);
        
        var monitorId = await CreateMonitorAndGetIdAsync(factory);
        var monitor = await dbContext.Monitors.FindAsync([monitorId], TestContext.Current.CancellationToken);
        Assert.NotNull(monitor);

        monitor.IsEnabled = true;
        monitor.NextCheckAt = DateTime.UtcNow.AddMinutes(5);
        
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
        var processor = CreateProcessor(scope.ServiceProvider, dbContext, httpClient);

        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestData.TestPassword);
        
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
        var validMonitorLocation = await TestData.CreateMonitor(client);
        var validMonitorId = int.Parse(validMonitorLocation!.ToString().TrimEnd('/').Split('/')[^1]);

        await MakeMonitorDueAsync(dbContext, invalidMonitorId, true, TestContext.Current.CancellationToken);
        await MakeMonitorDueAsync(dbContext, validMonitorId, true, TestContext.Current.CancellationToken);
        
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

        Assert.Equal(checkResult.CheckedAt.AddSeconds(TestData.TestMonitor.IntervalSeconds), validMonitor.NextCheckAt);
        Assert.True(validMonitor.IsEnabled);
    }
    
    [Theory]
    [InlineData(typeof(SuccessHttpHandler), ServiceStatus.Up)]
    [InlineData(typeof(ServiceUnavailableHandler), ServiceStatus.Up)]
    [InlineData(typeof(TimeoutHttpHandler), ServiceStatus.Down)]
    [InlineData(typeof(NetworkFailureHttpHandler), ServiceStatus.Down)]
    public async Task MonitorOutcome_SetsExpectedServiceStatus(Type handlerType, ServiceStatus expectedStatus)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = (HttpMessageHandler)Activator.CreateInstance(handlerType)!;
        using var httpClient = new HttpClient(handler);
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var processor = CreateProcessor(scope.ServiceProvider, dbContext, httpClient);

        int monitorId = await CreateMonitorAndGetIdAsync(factory);
        await MakeMonitorDueAsync(dbContext, monitorId, true, TestContext.Current.CancellationToken);
        var monitor = await dbContext.Monitors.FindAsync([monitorId], TestContext.Current.CancellationToken);
        
        var serviceStatusHandler = scope.ServiceProvider.GetRequiredService<ServiceStatusHandler>();

        var checkResults = await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        var checkResult = Assert.Single(checkResults);
        await serviceStatusHandler.ConsiderServiceStatus(checkResult, TestContext.Current.CancellationToken);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var service = await verificationDb.Services.AsNoTracking()
            .SingleAsync(s => s.ServiceId == monitor.ServiceId, TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, service.ServiceStatus);
    }
    
    private static async Task<int> CreateMonitorAndGetIdAsync(PulseWebApplicationFactory<Program> factory)
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestData.TestPassword);
        
        var location = await TestData.CreateMonitor(client);
        var monitorId = int.Parse(location!.ToString().TrimEnd('/').Split('/')[^1]);
        return monitorId;
    }
    
    private static DueMonitorProcessor CreateProcessor(IServiceProvider services, PulseDbContext dbContext, HttpClient httpClient)
    {
        var writer = services.GetRequiredService<CheckResultWriter>();
        var logger = services.GetRequiredService<ILogger<DueMonitorProcessor>>();
        var checker = new HttpMonitorChecker(httpClient, NullLogger<HttpMonitorChecker>.Instance);

        return new DueMonitorProcessor(dbContext, checker, writer, logger);
    }
    
    private static async Task MakeMonitorDueAsync(PulseDbContext db, int monitorId, bool isEnabled, CancellationToken token)
    {
        var monitor = await db.Monitors.FindAsync([monitorId], token);
        Assert.NotNull(monitor);

        monitor.IsEnabled = isEnabled;
        monitor.NextCheckAt = DateTime.UtcNow.AddMinutes(-5);

        await db.SaveChangesAsync(token);
    }
}

public sealed class SuccessHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        return Task.FromResult(response);
    }
}

public sealed class TimeoutHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new OperationCanceledException();
    }
}

public sealed class NetworkFailureHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new HttpRequestException();
    }
}

public sealed class ServiceUnavailableHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        return Task.FromResult(response);
    }
}