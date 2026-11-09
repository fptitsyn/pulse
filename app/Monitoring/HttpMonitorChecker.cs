using System.Diagnostics;
using System.Net;
using app.Models;

namespace app.Monitoring;

public class HttpMonitorChecker
{
    private readonly HttpClient _client;
    private readonly ILogger<HttpMonitorChecker> _logger;

    public HttpMonitorChecker(HttpClient client, ILogger<HttpMonitorChecker> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task<HttpCheckOutcome> SendRequestAsync(Uri uri, HttpMethod method, CancellationToken token)
    {
        using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, uri);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, token);
            var outcome = response.IsSuccessStatusCode ? CheckOutcomeType.Success : CheckOutcomeType.HttpError;
            return BuildHttpCheckOutcome(stopwatch, response.StatusCode, outcome);
        }
        catch (HttpRequestException e)
        {
            _logger.LogDebug(e, "HTTP monitor request failed. Error category: {ErrorCategory}", e.HttpRequestError);
            
            return BuildHttpCheckOutcome(stopwatch, null, CheckOutcomeType.NetworkError);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException e)
        {
            _logger.LogDebug(e, "The request timed out.");
            return BuildHttpCheckOutcome(stopwatch, null, CheckOutcomeType.Timeout);
        }
    }

    private static HttpCheckOutcome BuildHttpCheckOutcome(Stopwatch stopwatch, HttpStatusCode? statusCode, CheckOutcomeType outcome)
    {
        DateTime now = DateTime.UtcNow;
        stopwatch.Stop();
        int duration = (int)stopwatch.ElapsedMilliseconds;
        var checkOutcome = new HttpCheckOutcome(now, duration, statusCode, outcome);
        
        return checkOutcome;
    }
}

public record HttpCheckOutcome(DateTime CheckedAt, int DurationMs, HttpStatusCode? HttpStatusCode, CheckOutcomeType Outcome);