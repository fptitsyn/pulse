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
            TestHelper.TestPassword);

        using var getResponse = await client.GetAsync("/services", TestContext.Current.CancellationToken);
        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        Assert.Equal(0, json.RootElement.GetArrayLength());

        using var postMonitorResponse = await client.PostAsJsonAsync($"/services/{serviceId}/monitors/",
            TestHelper.NewMonitor(), TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.NotFound, postMonitorResponse.StatusCode);
    }

    [Fact]
    public async Task CreateMonitor_WithValidData_IsCreatedAtLocation()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestHelper.TestPassword);
        
        var monitorLocation = await TestHelper.CreateMonitorAsync(client);

        using var getResponse = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var path = json.RootElement.GetProperty("path").GetString();
        var intervalSeconds = json.RootElement.GetProperty("intervalSeconds").GetInt32();
        var httpMethod = json.RootElement.GetProperty("httpMethod").GetString();
        
        Assert.Equal(TestHelper.NewMonitor().Path, path);
        Assert.Equal(TestHelper.NewMonitor().IntervalSeconds, intervalSeconds);
        Assert.Equal(TestHelper.NewMonitor().HttpMethod, httpMethod);
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
            TestHelper.TestPassword);

        MonitorDto initialMonitor = new()
        {
            Path = "testPath",
            IntervalSeconds = 3,
            HttpMethod = "HEAD",
            IsEnabled = true
        };

        var monitorLocation = await TestHelper.CreateMonitorAsync(client, initialMonitor);

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
        var updatedHttpMethod = json.RootElement.GetProperty("httpMethod").GetString();
        
        Assert.Equal(path, updatedPath);
        Assert.Equal(intervalSeconds, updatedIntervalSeconds);
        Assert.Equal(httpMethod, updatedHttpMethod);
    }

    [Fact]
    public async Task DeleteMonitor_IsSuccessful()
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestHelper.TestPassword);

        var monitorLocation = await TestHelper.CreateMonitorAsync(client);

        using var deleteMonitor = await client.DeleteAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteMonitor.StatusCode);
        
        using var tryGetDeletedMonitor = await client.GetAsync(monitorLocation, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, tryGetDeletedMonitor.StatusCode);
    }

    [Fact]
    public async Task GetMonitor_OwnedByAnotherUser_ReturnsNotFound()
    {
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestHelper.TestPassword);
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestHelper.TestPassword);
        
        var monitorLocationA = await TestHelper.CreateMonitorAsync(clientA);
        var monitorLocationB = await TestHelper.CreateMonitorAsync(clientB);

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
    public async Task GetMonitors_ForNonExistingService_ReturnsNotFound(int offset)
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestHelper.TestPassword);

        using var servicesResponse = await client.PostAsJsonAsync("/services", TestHelper.NewService(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, servicesResponse.StatusCode);

        var serviceLocation = servicesResponse.Headers.Location;
        Assert.NotNull(serviceLocation);

        int createdServiceId = int.Parse(serviceLocation.ToString().TrimEnd('/').Split('/')[^1]);
        int nonexistentServiceId = createdServiceId + offset;

        using var response = await client.GetAsync($"/services/{nonexistentServiceId}/monitors/",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetMonitor_WithInvalidServiceId_ReturnsBadRequest(int serviceId)
    {
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail,
            TestHelper.TestPassword);

        using var servicesResponse = await client.PostAsJsonAsync("/services", TestHelper.NewService(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, servicesResponse.StatusCode);
        var serviceLocation = servicesResponse.Headers.Location;
        
        using var successfulGetResponse = await client.GetAsync($"{serviceLocation}/monitors", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, successfulGetResponse.StatusCode);
        
        using var badGetResponse = await client.GetAsync($"services/{serviceId}/monitors/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, badGetResponse.StatusCode);
    }

    [Fact]
    public async Task GetAllMonitors_ForCurrentUser_ReturnsOnlyCurrentUserMonitors()
    {
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestHelper.TestPassword);
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestHelper.TestPassword);
        
        using var getResponseInitial = await clientA.GetAsync("/monitors", TestContext.Current.CancellationToken);
        getResponseInitial.EnsureSuccessStatusCode();
        using var initialJson = await getResponseInitial.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        Assert.Equal(0, initialJson?.RootElement.GetArrayLength());
        
        await TestHelper.CreateMonitorAsync(clientA);
        await TestHelper.CreateMonitorAsync(clientA);
        await TestHelper.CreateMonitorAsync(clientB);

        using var getResponseFinal = await clientA.GetAsync("/monitors", TestContext.Current.CancellationToken);
        getResponseFinal.EnsureSuccessStatusCode();
        using var json = await getResponseFinal.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        
        Assert.Equal(2, json?.RootElement.GetArrayLength());
    }
}