using System.Net;
using System.Net.Http.Json;
using app.Models;

namespace Pulse.IntegrationTests.Helpers;

public static class TestData
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
}