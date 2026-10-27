using System.Net;
using app.Models;

namespace app.Monitoring;

public record HttpCheckOutcome(DateTime CheckedAt, int DurationMs, HttpStatusCode? HttpStatusCode, string Outcome);