using System.ComponentModel.DataAnnotations;

namespace app.Models;

public class Service
{
    public int ServiceId { get; set; }

    public int ApplicationUserId { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string Name { get; set; } = null!;
    
    public virtual ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();

    public virtual ServiceStatus ServiceStatus { get; set; } = ServiceStatus.Unknown;

    public virtual ApplicationUser User { get; set; } = null!;
}

public class ServiceDto
{
    [Required]
    [Url]
    public required string BaseUrl { get; set; }

    [Required]
    [StringLength(30)]
    public required string Name { get; set; }
}

public enum ServiceStatus
{
    Unknown = -1,
    Up = 1,
    Down = 0
}
