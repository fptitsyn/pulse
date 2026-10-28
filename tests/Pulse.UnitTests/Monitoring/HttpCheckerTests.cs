using System.Net;
using app.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pulse.UnitTests.Monitoring;

public class HttpCheckerTests
{
    [Fact]
    public async Task SendGetRequest_ReturnsValidCheckOutcome()
    {
        using var client = new HttpClient(new FakeHttpHandler());

        var checker = new HttpMonitorChecker(client, NullLogger<HttpMonitorChecker>.Instance);
        Uri uri = new Uri("http://example.com");
        HttpMethod method = HttpMethod.Head;
        
        HttpCheckOutcome checkOutcome = await checker.SendRequestAsync(uri, method, TestContext.Current.CancellationToken);
        
        Assert.Equal(HttpStatusCode.OK, checkOutcome.HttpStatusCode);
        Assert.Equal(HttpMonitorChecker.Success, checkOutcome.Outcome);
    }
}

public sealed class FakeHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(
            HttpStatusCode.OK);

        return Task.FromResult(response);
    }
}