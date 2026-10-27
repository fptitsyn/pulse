using System.ComponentModel.DataAnnotations;

namespace app.Models;

public class Monitor
{
    public int MonitorId { get; set; }

    public int ServiceId { get; set; }

    public string Path { get; set; } = null!;

    public int IntervalSeconds { get; set; }

    public short HttpMethodId { get; set; }

    public bool IsEnabled { get; set; } = true;
    
    public DateTime NextCheckedAt { get; set; }

    public virtual ICollection<CheckResult> CheckResults { get; set; } = new List<CheckResult>();

    public virtual HttpMethod HttpMethod { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}

public class MonitorDto
{
    [Required]
    public required string Path { get; set; }
    [Required]
    [Range(1, 60 * 60 * 24)]
    public int IntervalSeconds { get; set; }
    [Range(1, 6)]
    public short HttpMethodId { get; set; }
}