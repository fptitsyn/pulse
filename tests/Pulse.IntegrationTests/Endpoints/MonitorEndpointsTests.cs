using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Endpoints;

public class MonitorEndpointsTests : IClassFixture<PulseWebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MonitorEndpointsTests(PulseWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task CreateMonitor_ForNonExistingService_ReturnsNotFound(int serviceId)
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);

        using var getResponse = await client.GetAsync("/services", TestContext.Current.CancellationToken);
        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        Assert.Equal(0, json.RootElement.GetArrayLength());

        using var postMonitorResponse = await client.PostAsJsonAsync($"/services/{serviceId}/monitors/",
            TestConstants.TestMonitor, TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.NotFound, postMonitorResponse.StatusCode);
    }

    [Fact]
    public async Task CreateMonitor_WithValidData_IsAvailableAtLocation()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);

        using var servicesResponse = await client.PostAsJsonAsync("/services", TestConstants.TestService,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, servicesResponse.StatusCode);
        var serviceLocation = servicesResponse.Headers.Location;

        using var monitorResponse = await client.PostAsJsonAsync($"{serviceLocation}/monitors", TestConstants.TestMonitor,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, monitorResponse.StatusCode);
        var monitorLocation = monitorResponse.Headers.Location;

        using var getResponse = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}