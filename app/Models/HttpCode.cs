namespace app.Models;

public partial class HttpCode
{
    public short HttpCodeId { get; set; }

    public short Code { get; set; }

    public virtual ICollection<CheckResult> CheckResults { get; set; } = new List<CheckResult>();
}
