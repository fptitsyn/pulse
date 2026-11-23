using System.Net;
using System.Text.Json;
using app.Models;
using Microsoft.Extensions.DependencyInjection;
using Pulse.IntegrationTests.Fixtures;
using Pulse.IntegrationTests.Helpers;

namespace Pulse.IntegrationTests.Endpoints;

public class CheckResultEndpointTests(PulseWebApplicationFactory<Program> factory)
    : IClassFixture<PulseWebApplicationFactory<Program>>
{
    [Fact]
    public async Task PostCheckResult_ReturnsMethodNotAllowed()
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

        var location = await TestHelper.CreateMonitorAsync(client);
        int monitorId = int.Parse(location.ToString().TrimEnd('/').Split('/')[^1]);
        await CreateCheckResultAsync(factory, monitorId);
        await CreateCheckResultAsync(factory, monitorId);

        using var getResponse = await client.GetAsync("/check-results", TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();
        using var json =
            await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var checkResultsCount = json.RootElement.GetArrayLength();

        Assert.Equal(2, checkResultsCount);
    }

    [Fact]
    public async Task DeleteCheckResult_ByOwner_Success()
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);

        var location = await TestHelper.CreateMonitorAsync(client);
        int monitorId = int.Parse(location.ToString().TrimEnd('/').Split('/')[^1]);
        List<int> ids = [await CreateCheckResultAsync(factory, monitorId), await CreateCheckResultAsync(factory, monitorId)];

        using var deleteResponse =
            await client.DeleteAsync($"{location}/check-results/{ids[0]}", TestContext.Current.CancellationToken);

        using var getAfterDeleteResponse = await client.GetAsync("/check-results", TestContext.Current.CancellationToken);
        getAfterDeleteResponse.EnsureSuccessStatusCode();
        
        using var afterJson = await getAfterDeleteResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var checkResultsCount = afterJson.RootElement.GetArrayLength();
        int afterId = afterJson.RootElement[0].GetProperty("checkResultId").GetInt32();

        Assert.Equal(1, checkResultsCount);
        Assert.Equal(ids[1], afterId);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetMonitorCheckResults_FromAnotherUser_ReturnsNotFound()
    {
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmailA, TestHelper.TestPassword);
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmailB, TestHelper.TestPassword);

        var monitorLocationA = await TestHelper.CreateMonitorAsync(clientA);
        int monitorIdA = int.Parse(monitorLocationA.ToString().TrimEnd('/').Split('/')[^1]);
        var monitorLocationB = await TestHelper.CreateMonitorAsync(clientB);
        int monitorIdB = int.Parse(monitorLocationB.ToString().TrimEnd('/').Split('/')[^1]);

        int checkResultIdA = await CreateCheckResultAsync(factory, monitorIdA);
        int checkResultIdB = await CreateCheckResultAsync(factory, monitorIdB);

        using var getA = await clientA.GetAsync($"{monitorLocationA}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getA.StatusCode);
        
        using var getAWrong = await clientA.GetAsync($"{monitorLocationB}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getAWrong.StatusCode);

        using var getB = await clientB.GetAsync($"{monitorLocationB}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getB.StatusCode);
        
        using var getBWrong = await clientB.GetAsync($"{monitorLocationA}/check-results", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getBWrong.StatusCode);
        
        using var jsonA = await getA.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        var resultA = Assert.Single(jsonA!.RootElement.EnumerateArray());

        Assert.Equal(checkResultIdA, resultA.GetProperty("checkResultId").GetInt32());
        Assert.NotEqual(checkResultIdB, resultA.GetProperty("checkResultId").GetInt32());
    }

    [Fact]
    public async Task GetDeletedCheckResult_ReturnsNotFound()
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);

        var location = await TestHelper.CreateMonitorAsync(client);
        int monitorId = int.Parse(location.ToString().TrimEnd('/').Split('/')[^1]);
        int checkResultId = await CreateCheckResultAsync(factory, monitorId);

        using var getResponse = await client.GetAsync("/check-results", TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await client.DeleteAsync($"{location}/check-results/{checkResultId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var getAfterResponse = await client.GetAsync($"{location}/check-results/{checkResultId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getAfterResponse.StatusCode);
    }

    [Fact]
    public async Task GetCheckResult_AsUnauthenticatedUser_ReturnsUnauthorized()
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);

        var location = await TestHelper.CreateMonitorAsync(client);
        int monitorId = int.Parse(location.ToString().TrimEnd('/').Split('/')[^1]);
        await CreateCheckResultAsync(factory, monitorId);

        var unauthorizedClient = factory.CreateClient();
        using var getResponse =
            await unauthorizedClient.GetAsync($"{location}/check-results", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCheckResult_FromAnotherUser_ReturnsNotFound()
    {
        string testEmailA = $"test-{Guid.NewGuid():N}@example.com";
        var clientA = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmailA, TestHelper.TestPassword);
        string testEmailB = $"test-{Guid.NewGuid():N}@example.com";
        var clientB = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmailB, TestHelper.TestPassword);

        var monitorLocationA = await TestHelper.CreateMonitorAsync(clientA);
        int monitorIdA = int.Parse(monitorLocationA.ToString().TrimEnd('/').Split('/')[^1]);

        int checkResIdA = await CreateCheckResultAsync(factory, monitorIdA);

        using var tryDeleteA = await clientB.DeleteAsync($"{monitorLocationA}/check-results/{checkResIdA}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, tryDeleteA.StatusCode);

        using var getA = await clientA.GetAsync($"{monitorLocationA}/check-results/{checkResIdA}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getA.StatusCode);
    }

    [Fact]
    public async Task GetSingleCheckResult_ReturnsOK()
    {
        var testEmail = $"test-{Guid.NewGuid():N}@example.com";
        var client = await TestAuthentication.RegisterAndLoginUserAsync(factory.CreateClient(), testEmail, TestHelper.TestPassword);

        var location = await TestHelper.CreateMonitorAsync(client);
        int monitorId = int.Parse(location.ToString().TrimEnd('/').Split('/')[^1]);
        int checkResultId = await CreateCheckResultAsync(factory, monitorId);

        using var getResponse = await client.GetAsync($"{location}/check-results/{checkResultId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        int duration = json.RootElement.GetProperty("durationMs").GetInt32();
        
        Assert.Equal(1, duration);
        Assert.Equal(checkResultId, json.RootElement.GetProperty("checkResultId").GetInt32());
        Assert.Equal(monitorId, json.RootElement.GetProperty("monitorId").GetInt32());
    }

    private static async Task<int> CreateCheckResultAsync(PulseWebApplicationFactory<Program> factory, int monitorId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var result = new CheckResult
        {
            MonitorId = monitorId,
            CheckedAt = DateTime.UtcNow,
            DurationMs = 1,
            Outcome = CheckOutcomeType.Success,
            HttpStatusCode = HttpStatusCode.OK
        };

        db.CheckResults.Add(result);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return result.CheckResultId;
    }
}