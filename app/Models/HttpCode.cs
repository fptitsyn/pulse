namespace app.Models;

public class HttpCode
{
    public short HttpCodeId { get; set; }

    public short Code { get; set; }

    public virtual ICollection<CheckResult> CheckResults { get; set; } = new List<CheckResult>();
}

public static class HttpCodeIds
{
    public const short Code200 = 1;
    public const short Code201 = 2;
    public const short Code204 = 3;
    public const short Code301 = 4;
    public const short Code302 = 5;
    public const short Code304 = 6;
    public const short Code400 = 7;
    public const short Code401 = 8;
    public const short Code403 = 9;
    public const short Code404 = 10;
    public const short Code500 = 11;
}