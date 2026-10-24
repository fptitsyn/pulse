using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pulse.IntegrationTests.Fixtures;

namespace Pulse.IntegrationTests.Services;

public class ServicesEndpointsTests : IClassFixture<PulseWebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ServicesEndpointsTests(PulseWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetServices_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/services", TestContext.Current.CancellationToken);
        
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

        string email = "test@example.com";
        string password = "TestPassword123!";
        
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };
        
        await userManager.CreateAsync(user, password);
        
        var loginRequest = new
        {
            email,
            password
        };
        
        // Act
        var response = await client.PostAsJsonAsync("/login?useCookies=false",
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
}