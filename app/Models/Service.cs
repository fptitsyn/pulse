namespace app.Models;

public partial class Service
{
    public int ServiceId { get; set; }

    public int UserId { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string Name { get; set; } = null!;
    
    public short ServiceStatusId { get; set; }

    public virtual ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();

    public virtual ServiceStatus ServiceStatus { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
