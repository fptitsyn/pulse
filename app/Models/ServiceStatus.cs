namespace app.Models;

public partial class ServiceStatus
{
    public short ServiceStatusId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
