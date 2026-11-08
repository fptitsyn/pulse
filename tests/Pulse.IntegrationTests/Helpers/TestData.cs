using System.Net;
using System.Net.Http.Json;
using app.Models;

namespace Pulse.IntegrationTests.Helpers;

public static class TestData
{
    public const string TestPassword = "TestPassword123!";

    public static readonly ServiceDto TestService = new()
    {
        Name = "testName",
        BaseUrl = "https://test.com"
    };
    
    public static readonly MonitorDto TestMonitor = new()
    {
        Path = "testPath",
        IntervalSeconds = 5,
        HttpMethod = "HEAD",
        IsEnabled = true
    };

    public static async Task<Uri?> CreateMonitor(HttpClient client, MonitorDto? monitor = null)
    {
        monitor ??= TestMonitor;
        
        using var servicesResponse = await client.PostAsJsonAsync("/services", TestService,
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