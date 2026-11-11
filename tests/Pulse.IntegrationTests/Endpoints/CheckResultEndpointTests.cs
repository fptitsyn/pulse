using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Endpoints;

public class CheckResultEndpointTests : IClassFixture<PulseWebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CheckResultEndpointTests(PulseWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnsurePostRequest_DoesNotProceed()
    {
        string testEmail = $"test{Guid.NewGuid()}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(_factory.CreateClient(), testEmail, TestData.TestPassword);

        var monitorLocation = await TestData.CreateMonitorAsync(client);
        using var postResponse = await client.PostAsync($"{monitorLocation}/check-results", null,
            TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
    }
}