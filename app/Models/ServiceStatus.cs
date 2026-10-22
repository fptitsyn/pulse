namespace app.Models;

public class ServiceStatus
{
    public short ServiceStatusId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}

public static class ServiceStatusIds
{
    public const short Unknown = 1;
    public const short Up = 2;
    public const short Down = 3;
}