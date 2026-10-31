using System.Net;

namespace app.Monitoring;

public record HttpCheckOutcome(DateTime CheckedAt, int DurationMs, HttpStatusCode? HttpStatusCode, string Outcome);