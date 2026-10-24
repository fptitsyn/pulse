using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Services;

public class ServicesEndpointsTests : IClassFixture<PulseWebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    
    private const string TestPassword = "TestPassword123!";
    private readonly ServiceDto _testService = new()
    {
        Name = "testName",
        BaseUrl = "http://test.com"
    };

    public ServicesEndpointsTests(PulseWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    // Auth
    [Fact]
    public async Task GetServices_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/services", TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        
        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail
        };
        
        var result = await userManager.CreateAsync(user, TestPassword);
        Assert.True(result.Succeeded);
        
        var loginRequest = new
        {
            email = testEmail,
            password = TestPassword
        };
        
        // Act
        using var response = await client.PostAsJsonAsync("/login?useCookies=false",
            loginRequest,
            TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        string? token = json?.RootElement
            .GetProperty("accessToken")
            .GetString();
        
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    // Service creation
    [Fact]
    public async Task CreateService_WithValidData_ReturnsCreated()
    {
        // Arrange
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail, TestPassword);
        
        // Act
        using var response = await client.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);

        var location = response.Headers.Location;
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(location);
        Assert.Matches(@"^/services/\d+$", location.ToString());
    }
    
    [Theory]
    [InlineData("/test-url.com")]
    [InlineData("http:/test-url.com")]
    [InlineData("htp://test-url.com")]
    [InlineData("htpp://test-url")]
    public async Task CreateService_WithInvalidUrl_ReturnsBadRequest(string baseUrl)
    {
        // Arrange
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail, TestPassword);
        
        ServiceDto serviceDto = new ServiceDto
        {
            Name = "testName",
            BaseUrl = baseUrl
        };
        
        // Act
        using var response = await client.PostAsJsonAsync("/services", serviceDto, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    // Ownership
    [Fact]
    public async Task GetServices_ReturnsOnlyCurrentUsersServices()
    {
        // Arrange
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestPassword);
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestPassword);

        using var getResponseInitial = await clientA.GetAsync("/services", TestContext.Current.CancellationToken);
        getResponseInitial.EnsureSuccessStatusCode();
        using var initialJson = await getResponseInitial.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        int initialCount = initialJson.RootElement.GetArrayLength();
        Assert.Equal(0, initialCount);
        
        using var responseA1 = await clientA.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseA1.EnsureSuccessStatusCode();
        
        using var responseA2 = await clientA.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseA2.EnsureSuccessStatusCode();
        
        using var responseB1 = await clientB.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseB1.EnsureSuccessStatusCode();
        
        // Act
        using var getResponseA = await clientA.GetAsync("/services", TestContext.Current.CancellationToken);
        getResponseA.EnsureSuccessStatusCode();

        using var json = await getResponseA.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        // Assert
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetService_OwnedByAnotherUser_ReturnsNotFound()
    {
        // Arrange
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestPassword);
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestPassword);
        
        using var responseA1 = await clientA.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseA1.EnsureSuccessStatusCode();
        
        using var responseB1 = await clientB.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseB1.EnsureSuccessStatusCode();
        var location = responseB1.Headers.Location;
        
        // Act
        using var getResponse = await clientA.GetAsync(location, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task UpdateService_OwnedByAnotherUser_ReturnsNotFound()
    {
        // Arrange
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestPassword);
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestPassword);
        
        using var responseA1 = await clientA.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseA1.EnsureSuccessStatusCode();
        
        using var responseB1 = await clientB.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseB1.EnsureSuccessStatusCode();
        var location = responseB1.Headers.Location;

        ServiceDto updatedServiceDto = new ServiceDto
        {
            Name = "UpdatedName",
            BaseUrl = "http://updated-test.com"
        };
        
        // Act
        using var putResponse = await clientA.PutAsJsonAsync(location, updatedServiceDto, TestContext.Current.CancellationToken);

        using var getResponseB = await clientB.GetAsync(location, TestContext.Current.CancellationToken);
        getResponseB.EnsureSuccessStatusCode();
        using var json = await getResponseB.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var serviceName = json.RootElement.GetProperty("name").GetString();
        var baseUrl = json.RootElement.GetProperty("baseUrl").GetString();
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        Assert.Equal(_testService.Name, serviceName);
        Assert.Equal(_testService.BaseUrl, baseUrl);
    }
    
    [Fact]
    public async Task DeleteService_OwnedByAnotherUser_ReturnsNotFound()
    {
        // Arrange
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailA, TestPassword);
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmailB, TestPassword);
        
        using var responseA1 = await clientA.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseA1.EnsureSuccessStatusCode();
        
        using var responseB1 = await clientB.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        responseB1.EnsureSuccessStatusCode();
        var location = responseB1.Headers.Location;
        
        // Act
        using var deleteResponse = await clientA.DeleteAsync(location, TestContext.Current.CancellationToken);
        using var getResponseB = await clientB.GetAsync(location, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponseB.StatusCode);
    }
    
    // CRUD
    [Theory]
    [InlineData("testName", "https://test-url.com")]
    [InlineData("testName", "http://pulse.com")]
    [InlineData("updatedName", "http://test.com")]
    [InlineData("testName", "http://test.com")]
    public async Task UpdateService_AsOwner_PersistsChanges(string name, string baseUrl)
    {
        // Arrange
        string testEmail = $"test{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail, TestPassword);

        ServiceDto serviceDto = new ServiceDto
        {
            Name = "testName",
            BaseUrl = "http://test.com"
        };

        using var postResponse = await client.PostAsJsonAsync("/services", serviceDto, TestContext.Current.CancellationToken);
        postResponse.EnsureSuccessStatusCode();
        var location = postResponse.Headers.Location;

        serviceDto.Name = name;
        serviceDto.BaseUrl = baseUrl;
        
        // Act
        using var putResponse = await client.PutAsJsonAsync(location, serviceDto, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        using var getResponse = await client.GetAsync(location, TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();

        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var finalName = json.RootElement
            .GetProperty("name")
            .GetString();
        var finalUrl = json.RootElement
            .GetProperty("baseUrl")
            .GetString();
        
        // Assert
        Assert.Equal(name, finalName);
        Assert.Equal(baseUrl, finalUrl);
    }

    [Fact]
    public async Task DeleteService_AsOwner_RemovesService()
    {
        // Arrange
        string testEmail = $"test{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail, TestPassword);

        using var postResponse = await client.PostAsJsonAsync("/services", _testService, TestContext.Current.CancellationToken);
        postResponse.EnsureSuccessStatusCode();
        var location = postResponse.Headers.Location;
        
        // Act
        using var deleteResponse = await client.DeleteAsync(location, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getResponse = await client.GetAsync(location, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}