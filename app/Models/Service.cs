namespace app.Models;

public class Service
{
    public int ServiceId { get; set; }

    public int ApplicationUserId { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string Name { get; set; } = null!;
    
    public short ServiceStatusId { get; set; }

    public virtual ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();

    public virtual ServiceStatus ServiceStatus { get; set; } = null!;

    public virtual ApplicationUser User { get; set; } = null!;
}

public class ServiceDto
{
    public string BaseUrl { get; set; } = null!;

    public string Name { get; set; } = null!;
}
