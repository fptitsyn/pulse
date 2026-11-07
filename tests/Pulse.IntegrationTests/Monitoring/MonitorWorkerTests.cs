using app.Models;
using app.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pulse.IntegrationTests.Endpoints;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;
using Pulse.UnitTests.Monitoring;   

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
        
        var entity = await dbContext.Monitors.FindAsync([monitorId], TestContext.Current.CancellationToken);
        Assert.NotNull(entity);

        entity.NextCheckAt = DateTime.UtcNow.AddMinutes(-5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        
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

    [Theory]
    [InlineData("https://base/", "/path")]
    [InlineData("https://base", "/path")]
    [InlineData("https://base/", "path")]
    [InlineData("https://base", "path")]
    public void BaseUrlAndPath_CombinedSuccessfully(string baseUrl, string path)
    {
        Uri validUri = new Uri("https://base/path");
        Uri uri = CustomUriBuilder.Build(baseUrl, path);
        Assert.Equal(validUri, uri);
    }
}