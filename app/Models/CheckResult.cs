namespace app.Models;

public partial class CheckResult
{
    public int CheckResultId { get; set; }

    public int MonitorId { get; set; }

    public DateTime CheckedAt { get; set; }

    public short? HttpCodeId { get; set; }

    public string Outcome { get; set; } = null!;

    public int DurationMs { get; set; }

    public virtual HttpCode? HttpCode { get; set; }

    public virtual Monitor Monitor { get; set; } = null!;
}
