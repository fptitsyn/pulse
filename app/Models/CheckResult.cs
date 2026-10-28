using System.Net;

namespace app.Models;

public class CheckResult
{
    public int CheckResultId { get; set; }

    public int MonitorId { get; set; }

    public DateTime CheckedAt { get; set; }

    public HttpStatusCode? HttpStatusCode { get; set; }

    public string Outcome { get; set; } = null!;

    public int DurationMs { get; set; }

    public virtual Monitor Monitor { get; set; } = null!;
}
