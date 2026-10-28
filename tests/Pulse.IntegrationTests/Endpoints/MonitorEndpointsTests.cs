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
        var httpMethod = json.RootElement.GetProperty("method").GetString();
        
        Assert.Equal(TestConstants.TestMonitor.Path, path);
        Assert.Equal(TestConstants.TestMonitor.IntervalSeconds, intervalSeconds);
        Assert.Equal(TestConstants.TestMonitor.HttpMethod, httpMethod);
    }

    [Theory]
    [InlineData("testPath", 3, "HEAD")]
    [InlineData("updatedPath", 5, "GET")]
    [InlineData("testPath", 5, "HEAD")]
    [InlineData("testPath", 3, "GET")]
    [InlineData("updatedPath", 3, "HEAD")]
    public async Task UpdateMonitor_WithValidData_IsSuccessful(string path, int intervalSeconds, string httpMethod)
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);

        MonitorDto initialMonitor = new()
        {
            Path = "testPath",
            IntervalSeconds = 3,
            HttpMethod = "HEAD"
        };

        var monitorLocation = await CreateMonitor(client, initialMonitor);

        initialMonitor.Path = path;
        initialMonitor.IntervalSeconds = intervalSeconds;
        initialMonitor.HttpMethod = httpMethod;

        using var updateMonitorResponse =
            await client.PutAsJsonAsync(monitorLocation, initialMonitor, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, updateMonitorResponse.StatusCode);

        using var getUpdatedMonitor = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        
        using var json = await getUpdatedMonitor.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        
        var updatedPath = json.RootElement.GetProperty("path").GetString();
        var updatedIntervalSeconds = json.RootElement.GetProperty("intervalSeconds").GetInt32();
        var updatedHttpMethod = json.RootElement.GetProperty("method").GetString();
        
        Assert.Equal(path, updatedPath);
        Assert.Equal(intervalSeconds, updatedIntervalSeconds);
        Assert.Equal(httpMethod, updatedHttpMethod);
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
    public async Task GetMonitor_OwnedByAnotherUser_ReturnsNotFound()
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
        Assert.Equal(HttpStatusCode.NotFound, getBWrong.StatusCode);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task GetMonitors_ForNonExistingService_ReturnsNotFound(int serviceId)
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
        
        using var getAllMonitors =
            await client.GetAsync($"{serviceLocation}/monitors", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getAllMonitors.StatusCode);

        using var json = await getAllMonitors.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        Assert.Equal(1, json.RootElement.GetArrayLength());

        using var getWrongServiceMonitors = await client.GetAsync($"/services/{serviceId}/monitors/",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getWrongServiceMonitors.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetMonitor_WithInvalidServiceId_ReturnsBadRequest(int serviceId)
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestConstants.TestPassword);

        using var servicesResponse = await client.PostAsJsonAsync("/services", TestConstants.TestService,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, servicesResponse.StatusCode);
        var serviceLocation = servicesResponse.Headers.Location;
        
        using var successfulGetResponse = await client.GetAsync($"{serviceLocation}/monitors", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, successfulGetResponse.StatusCode);
        
        using var badGetResponse = await client.GetAsync($"services/{serviceId}/monitors/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, badGetResponse.StatusCode);
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