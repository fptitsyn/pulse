using System.Net;
using app.Models;
using app.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pulse.IntegrationTests.Endpoints;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Monitoring;

public class MonitorWorkerTests(PulseWebApplicationFactory<Program> factory) : IClassFixture<PulseWebApplicationFactory<Program>>
{
    [Fact]
    public async Task MonitorProcessor_WithOverdueMonitor_SavesExactlyOneResult()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail,
            TestConstants.TestPassword);
        
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        
        var checker = new HttpMonitorChecker(httpClient, NullLogger<HttpMonitorChecker>.Instance);
        var writer = scope.ServiceProvider.GetRequiredService<CheckResultWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        
        var processor = new DueMonitorProcessor(dbContext, checker, writer, NullLogger<DueMonitorProcessor>.Instance);

        var location = await MonitorEndpointsTests.CreateMonitor(client);
        var monitorId = int.Parse(location!.ToString().TrimEnd('/').Split('/')[^1]);

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

        Assert.Equal(checkResult.CheckedAt.AddSeconds(TestConstants.TestMonitor.IntervalSeconds), updatedMonitor.NextCheckAt);
    }

    [Fact]
    public async Task DisabledMonitor_DoesNotExecute()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail,
            TestConstants.TestPassword);
        
        await using var scope = factory.Services.CreateAsyncScope();
        using var httpClient = new HttpClient(new SuccessHttpHandler());
        
        var checker = new HttpMonitorChecker(httpClient, NullLogger<HttpMonitorChecker>.Instance);
        var writer = scope.ServiceProvider.GetRequiredService<CheckResultWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        
        var processor = new DueMonitorProcessor(dbContext, checker, writer, NullLogger<DueMonitorProcessor>.Instance);
        
        var location = await MonitorEndpointsTests.CreateMonitor(client);
        var monitorId = int.Parse(location!.ToString().TrimEnd('/').Split('/')[^1]);

        await MakeMonitorDueAsync(dbContext, monitorId, false, TestContext.Current.CancellationToken);
        
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var checkResults = await verificationDb.CheckResults.AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(checkResults);

        var updatedMonitor = await verificationDb.Monitors.AsNoTracking()
            .SingleAsync(m => m.MonitorId == monitorId, TestContext.Current.CancellationToken);

        Assert.False(updatedMonitor.IsEnabled);
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