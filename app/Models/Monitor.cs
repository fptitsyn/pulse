using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace app.Models;

public class Monitor
{
    public int MonitorId { get; set; }

    public int ServiceId { get; set; }

    public string Path { get; set; } = null!;

    public int IntervalSeconds { get; set; }

    public HttpMethod HttpMethod { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;
    
    public DateTime NextCheckAt { get; set; }

    public virtual ICollection<CheckResult> CheckResults { get; set; } = new List<CheckResult>();

    public virtual Service Service { get; set; } = null!;
}

public class MonitorDto
{
    [Required]
    public required string Path { get; set; }
    [Required]
    [Range(1, 60 * 60 * 24)]
    public int IntervalSeconds { get; set; }
    // Slashes will be trimmed with TrimStart()
    [Required]
    public string HttpMethod { get; set; }
}