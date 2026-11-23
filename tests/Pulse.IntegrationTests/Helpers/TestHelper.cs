using System.Net;
using app.Models;
using app.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pulse.IntegrationTests.Fixtures;

namespace Pulse.IntegrationTests.Helpers;

public static class TestHelper
{
    public const string TestPassword = "TestPassword123!";

    public static ServiceDto NewService() => new() { Name = "testName", BaseUrl = "https://test.com" };
    public static MonitorDto NewMonitor() => new() { Path = "testPath", IntervalSeconds = 5, HttpMethod = "HEAD", IsEnabled = true };

    public static async Task<Uri?> CreateMonitorAsync(HttpClient client, MonitorDto? monitor = null)
    {
        monitor ??= NewMonitor();
        
        using var servicesResponse = await client.PostAsJsonAsync("/services", NewService(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, servicesResponse.StatusCode);
        var serviceLocation = servicesResponse.Headers.Location;
        
        using var monitorResponse = await client.PostAsJsonAsync($"{serviceLocation}/monitors", monitor,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, monitorResponse.StatusCode);
        var monitorLocation = monitorResponse.Headers.Location;

        return monitorLocation;
    }

    public static DueMonitorProcessor CreateProcessor(IServiceProvider services, PulseDbContext dbContext, HttpClient httpClient)
    {
        var writer = services.GetRequiredService<CheckResultWriter>();
        var processorLogger = services.GetRequiredService<ILogger<DueMonitorProcessor>>();
        var checker = new HttpMonitorChecker(httpClient, NullLogger<HttpMonitorChecker>.Instance);

        return new DueMonitorProcessor(dbContext, checker, writer, processorLogger);
    }

    public static async Task MakeMonitorDueAsync(PulseDbContext dbContext, int monitorId, bool isEnabled, CancellationToken token)
    {
        var monitor = await dbContext.Monitors.FindAsync([monitorId], token);
        Assert.NotNull(monitor);

        monitor.IsEnabled = isEnabled;
        monitor.NextCheckAt = DateTime.UtcNow.AddMinutes(-5);

        await dbContext.SaveChangesAsync(token);
    }

    public static async Task<int> CreateMonitorAndGetIdAsync(PulseWebApplicationFactory<Program> factory)
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);
        
        var location = await CreateMonitorAsync(client);
        var monitorId = int.Parse(location!.ToString().TrimEnd('/').Split('/')[^1]);
        return monitorId;
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

public sealed class NotFoundHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        return Task.FromResult(response);
    }
}