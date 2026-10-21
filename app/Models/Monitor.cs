namespace app.Models;

public class Monitor
{
    public int MonitorId { get; set; }

    public int ServiceId { get; set; }

    public string Path { get; set; } = null!;

    public int IntervalSeconds { get; set; }

    public short HttpMethodId { get; set; }

    public virtual ICollection<CheckResult> CheckResults { get; set; } = new List<CheckResult>();

    public virtual HttpMethod HttpMethod { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
