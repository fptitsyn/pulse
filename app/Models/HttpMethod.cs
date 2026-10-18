namespace app.Models;

public partial class HttpMethod
{
    public short HttpMethodId { get; set; }

    public string Method { get; set; } = null!;

    public virtual ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();
}
