namespace app.Models;

public class HttpMethod
{
    public short HttpMethodId { get; set; }

    public string Method { get; set; } = null!;

    public virtual ICollection<Monitor> Monitors { get; set; } = new List<Monitor>();
}

public static class HttpMethodIds
{
    public const short Head = 1;
    public const short Get = 2;
    public const short Post = 3;
    public const short Put = 4;
    public const short Patch = 5;
    public const short Delete = 6;
}