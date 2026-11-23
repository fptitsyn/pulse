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
    public async Task EnsurePostRequest_DoesNotProceed()
    {
        string testEmail = $"test{Guid.NewGuid()}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);

        var monitorLocation = await TestHelper.CreateMonitorAsync(client);
        using var postResponse = await client.PostAsync($"{monitorLocation}/check-results", null,
            TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
    }
    
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
    public async Task DeleteCheckResult_ByOwner_Success()
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
        List<int> ids = json.RootElement.EnumerateArray().Select(ch => ch.GetProperty("checkResultId").GetInt32())
            .ToList();

        using var deleteResponse =
            await client.DeleteAsync($"{location}/check-results/{ids[0]}", TestContext.Current.CancellationToken);
            
        using var getAfterDeleteResponse = await client.GetAsync("/check-results", TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();
        using var afterJson = await getAfterDeleteResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var checkResultsCount = afterJson.RootElement.GetArrayLength();
        
        Assert.Equal(1, checkResultsCount);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetCheckResult_FromAnotherUser_ReturnsNotFound()
    {
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmailA, TestHelper.TestPassword);
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmailB, TestHelper.TestPassword);
        
        var monitorLocationA = await TestHelper.CreateMonitorAsync(clientA);
        var monitorAId = int.Parse(monitorLocationA!.ToString().TrimEnd('/').Split('/')[^1]);
        var monitorLocationB = await TestHelper.CreateMonitorAsync(clientB);
        var monitorBId = int.Parse(monitorLocationB!.ToString().TrimEnd('/').Split('/')[^1]);

        await using var scope = factory.Services.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var processorA = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, clientA);
        var processorB = TestHelper.CreateProcessor(scope.ServiceProvider, dbContext, clientB);

        await TestHelper.MakeMonitorDueAsync(dbContext, monitorAId, true, TestContext.Current.CancellationToken);
        await TestHelper.MakeMonitorDueAsync(dbContext, monitorBId, true, TestContext.Current.CancellationToken);

        await processorA.ProcessDueAsync(TestContext.Current.CancellationToken);
        await processorB.ProcessDueAsync(TestContext.Current.CancellationToken);
        
        using var getA = await clientA.GetAsync($"{monitorLocationA}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getA.StatusCode);
        using var getAWrong = await clientA.GetAsync($"{monitorLocationB}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getAWrong.StatusCode);
        
        using var getB = await clientB.GetAsync($"{monitorLocationB}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getB.StatusCode);
        using var getBWrong = await clientB.GetAsync($"{monitorLocationA}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getBWrong.StatusCode);
    }

    [Fact]
    public async Task GetDeletedCheckResult_ReturnsNotFound()
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
        
        using var getResponse = await client.GetAsync("/check-results", TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();
        
        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        int id = json.RootElement[0].GetProperty("checkResultId").GetInt32();

        using var deleteResponse =
            await client.DeleteAsync($"{location}/check-results/{id}", TestContext.Current.CancellationToken);
            
        using var getAfterResponse = await client.GetAsync($"{location}/check-results/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getAfterResponse.StatusCode);
    }
}