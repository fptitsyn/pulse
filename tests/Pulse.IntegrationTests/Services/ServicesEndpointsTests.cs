using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    private const string TestEmailA = "testA@example.com";
    private const string TestEmailB = "testB@example.com";
    private const string TestPassword = "TestPassword123!";

    public ServicesEndpointsTests(PulseWebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
        
        await userManager.CreateAsync(user, TestPassword);
        
        var loginRequest = new
        {
            email = testEmail,
            password = TestPassword
        };
        
        // Act
        using var response = await client.PostAsJsonAsync("/login?useCookies=false",
            loginRequest,
            TestContext.Current.CancellationToken);
        
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        string? token = json?.RootElement
            .GetProperty("accessToken")
            .GetString();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    // Service creation
    [Fact]
    public async Task CreateService_WithValidData_ReturnsCreated()
    {
        // Arrange
        string testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail, TestPassword);

        ServiceDto serviceDto = new ServiceDto
        {
            Name = "testName",
            BaseUrl = "http://test-url.com"
        };
        
        // Act
        using var response = await client.PostAsJsonAsync("/services", serviceDto, TestContext.Current.CancellationToken);

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
        var clientA =
            await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), TestEmailA, TestPassword);
        var clientB =
            await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), TestEmailB, TestPassword);
        
        ServiceDto serviceDto = new ServiceDto
        {
            Name = "testName",
            BaseUrl = "http://test.com"
        };
        
        using var responseA1 = await clientA.PostAsJsonAsync("/services", serviceDto, TestContext.Current.CancellationToken);
        responseA1.EnsureSuccessStatusCode();
        
        using var responseA2 = await clientA.PostAsJsonAsync("/services", serviceDto, TestContext.Current.CancellationToken);
        responseA2.EnsureSuccessStatusCode();
        
        using var responseB1 = await clientB.PostAsJsonAsync("/services", serviceDto, TestContext.Current.CancellationToken);
        responseB1.EnsureSuccessStatusCode();
        
        // Act
        using var getResponseA = await clientA.GetAsync("/services", TestContext.Current.CancellationToken);
        getResponseA.EnsureSuccessStatusCode();

        using var json = await getResponseA.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        // Assert
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    public async Task GetService_OwnedByAnotherUser_ReturnsNotFound()
    {
        
    }
    
    public async Task UpdateService_OwnedByAnotherUser_ReturnsNotFound()
    {
        
    }
    
    public async Task DeleteService_OwnedByAnotherUser_ReturnsNotFound()
    {
        
    }
    
    // CRUD
    public async Task UpdateService_AsOwner_PersistsChanges()
    {
        
    }

    public async Task DeleteService_AsOwner_RemovesService()
    {
        
    }
}