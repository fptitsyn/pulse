using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using app.Models;
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
    public async Task CreateMonitor_WithValidData_IsCreatedAtLocation()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);
        
        var monitorLocation = await CreateMonitor(client);

        using var getResponse = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var path = json.RootElement.GetProperty("path").GetString();
        var intervalSeconds = json.RootElement.GetProperty("intervalSeconds").GetInt32();
        var httpMethodId = json.RootElement.GetProperty("httpMethodId").GetInt16();
        
        Assert.Equal(TestConstants.TestMonitor.Path, path);
        Assert.Equal(TestConstants.TestMonitor.IntervalSeconds, intervalSeconds);
        Assert.Equal(TestConstants.TestMonitor.HttpMethodId, httpMethodId);
    }

    [Theory]
    [InlineData("testPath", 3, 1)]
    [InlineData("updatedPath", 5, 2)]
    [InlineData("testPath", 5, 1)]
    [InlineData("testPath", 3, 2)]
    [InlineData("updatedPath", 3, 1)]
    public async Task UpdateMonitor_WithValidData_IsSuccessful(string path, int intervalSeconds, short httpMethodId)
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);

        MonitorDto initialMonitor = new()
        {
            Path = "testPath",
            IntervalSeconds = 3,
            HttpMethodId = 1
        };

        var monitorLocation = await CreateMonitor(client, initialMonitor);

        initialMonitor.Path = path;
        initialMonitor.IntervalSeconds = intervalSeconds;
        initialMonitor.HttpMethodId = httpMethodId;

        using var updateMonitorResponse =
            await client.PutAsJsonAsync(monitorLocation, initialMonitor, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, updateMonitorResponse.StatusCode);

        using var getUpdatedMonitor = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        
        using var json =
            await getUpdatedMonitor.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        
        var updatedPath = json.RootElement.GetProperty("path").GetString();
        var updatedIntervalSeconds = json.RootElement.GetProperty("intervalSeconds").GetInt32();
        var updatedHttpMethodId = json.RootElement.GetProperty("httpMethodId").GetInt16();
        
        Assert.Equal(path, updatedPath);
        Assert.Equal(intervalSeconds, updatedIntervalSeconds);
        Assert.Equal(httpMethodId, updatedHttpMethodId);
    }

    [Fact]
    public async Task DeleteMonitor_IsSuccessful()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);

        var monitorLocation = await CreateMonitor(client);

        using var deleteMonitor = await client.DeleteAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteMonitor.StatusCode);
        
        using var tryGetDeletedMonitor = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, tryGetDeletedMonitor.StatusCode);
    }

    [Fact]
    public async Task GetMonitor_ForInvalidService_ReturnsNotFound()
    {
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestConstants.TestPassword);
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestConstants.TestPassword);
        
        var monitorLocationA = await CreateMonitor(clientA);
        var monitorLocationB = await CreateMonitor(clientB);

        using var getA = await clientA.GetAsync(monitorLocationA, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getA.StatusCode);
        using var getAWrong = await clientA.GetAsync(monitorLocationB, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getAWrong.StatusCode);
        
        using var getB = await clientB.GetAsync(monitorLocationB, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getB.StatusCode);
        using var getBWrong = await clientB.GetAsync(monitorLocationA, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getBWrong.StatusCode);
    }

    private async Task<Uri?> CreateMonitor(HttpClient client, MonitorDto? monitor = null)
    {
        if (monitor == null)
        {
            monitor = TestConstants.TestMonitor;
        }
        
        using var servicesResponse = await client.PostAsJsonAsync("/services", TestConstants.TestService,
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