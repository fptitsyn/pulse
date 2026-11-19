using System.Net;
using System.Text.Json;
using app.Models;
using Microsoft.Extensions.DependencyInjection;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Endpoints;

public class CheckResultEndpointTests(PulseWebApplicationFactory<Program> factory) : IClassFixture<PulseWebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetCheckResults_ByOwner_Success()
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);
        
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        
        var processor = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, client);
        
        var location = await TestHelper.CreateMonitorAsync(client);
        var monitorId = int.Parse(location!.ToString().TrimEnd('/').Split('/')[^1]);

        await TestHelper.MakeMonitorDueAsync(dbContext, monitorId, true, TestContext.Current.CancellationToken);
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        await TestHelper.MakeMonitorDueAsync(dbContext, monitorId, true, TestContext.Current.CancellationToken);
        await processor.ProcessDueAsync(TestContext.Current.CancellationToken);

        using var getResponse = await client.GetAsync("/check-results", TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();
        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var checkResultsCount = json.RootElement.GetArrayLength();
        
        Assert.Equal(2, checkResultsCount);
    }
    
    [Fact]
    public async Task EnsurePostRequest_DoesNotProceed()
    {
        string testEmail = $"test{Guid.NewGuid()}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);

        var monitorLocation = await TestHelper.CreateMonitorAsync(client);
        using var postResponse = await client.PostAsync($"{monitorLocation}/check-results", null,
            TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
    }
}