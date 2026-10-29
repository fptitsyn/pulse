using System.Net;
using app.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pulse.UnitTests.Monitoring;

public class HttpCheckerTests
{
    [Fact]
    public async Task SendGetRequest_ReturnsValidCheckOutcome()
    {
        using var client = new HttpClient(new SuccessHttpHandler());

        var checker = new HttpMonitorChecker(client, NullLogger<HttpMonitorChecker>.Instance);
        Uri uri = new Uri("http://example.com");
        HttpMethod method = HttpMethod.Head;
        
        HttpCheckOutcome checkOutcome = await checker.SendRequestAsync(uri, method, TestContext.Current.CancellationToken);
        
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

        var checker = new HttpMonitorChecker(client, NullLogger<HttpMonitorChecker>.Instance);
        Uri uri = new Uri("http://example.com");
        HttpMethod method = HttpMethod.Head;
        
        HttpCheckOutcome checkOutcome = await checker.SendRequestAsync(uri, method, TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.HttpError, checkOutcome.Outcome);
    }
}

public sealed class SuccessHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        return Task.FromResult(response);
    }
}

public sealed class WaitingHttpHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

public sealed class ServiceUnavailableHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        return Task.FromResult(response);
    }
}