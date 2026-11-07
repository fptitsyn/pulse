using System.Net;
using app.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pulse.UnitTests.Monitoring;

public class HttpCheckerTests
{
    [Fact]
    public async Task SendGetRequest_ReturnsValidCheckOutcome()
    {
        var handler = new SuccessHttpHandler();
        using var client = new HttpClient(handler);

        string method = "GET";
        string uri = "http://example.com/index";
        HttpCheckOutcome checkOutcome = await CreateCheckOutcome(client, method, uri);
        
        Assert.Equal(HttpMethod.Get, handler.ReceivedMethod);
        Assert.Equal(new Uri(uri), handler.ReceivedUri);
        Assert.Equal(HttpStatusCode.OK, checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.Success, checkOutcome.Outcome);
    }

    [Fact]
    public async Task SendRequest_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var client = new HttpClient(new WaitingHttpHandler());
        using var cts = new CancellationTokenSource();

        var checker = new HttpMonitorChecker(client, NullLogger<HttpMonitorChecker>.Instance);

        Uri uri = new("http://example.com");
        HttpMethod method = HttpMethod.Head;

        Task<HttpCheckOutcome> checkTask = checker.SendRequestAsync(uri, method, cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await checkTask);
    }
    
    [Fact]
    public async Task SendRequest_ServerFailure_ReturnServiceUnavailable()
    {
        using var client = new HttpClient(new ServiceUnavailableHandler());

        HttpCheckOutcome checkOutcome = await CreateCheckOutcome(client);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.HttpError, checkOutcome.Outcome);
    }

    [Fact]
    public async Task SendRequest_WithMissingEndpoint_ReturnsNotFound()
    {
        using var client = new HttpClient(new NotFoundHandler());
        var checkOutcome = await CreateCheckOutcome(client, uriS: "http://example.x");
        
        Assert.Equal(HttpStatusCode.NotFound, checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.HttpError, checkOutcome.Outcome);
    }

    [Fact]
    public async Task SendRequest_ForTooLong_OutcomeTimeout()
    {
        using var client = new HttpClient(new TimeoutHttpHandler());
        var checkOutcome = await CreateCheckOutcome(client);
        
        Assert.Null(checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.RequestTimeout, checkOutcome.Outcome);
    }
    
    [Fact]
    public async Task SendRequest_RequestError_OutcomeNetworkError()
    {
        using var client = new HttpClient(new NetworkFailureHttpHandler());
        var checkOutcome = await CreateCheckOutcome(client);
        
        Assert.Null(checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.NetworkError, checkOutcome.Outcome);
    }
    
    private async Task<HttpCheckOutcome> CreateCheckOutcome(HttpClient client, string method = "HEAD", string uriS = "http://example.com")
    {
        var checker = new HttpMonitorChecker(client, NullLogger<HttpMonitorChecker>.Instance);
        HttpCheckOutcome checkOutcome = await checker.SendRequestAsync(new Uri(uriS), HttpMethod.Parse(method),
            TestContext.Current.CancellationToken);
        return checkOutcome;
    }
}