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

public class MonitorDto : IValidatableObject
{
    [Required]
    public required string Path { get; set; }
    [Required]
    [Range(1, 60 * 60 * 24)]
    public int IntervalSeconds { get; set; }
    [Required]
    public string HttpMethod { get; set; }

    private readonly Dictionary<string, HttpMethod> _allowedMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GET"] = System.Net.Http.HttpMethod.Get,
            ["POST"] = System.Net.Http.HttpMethod.Post,
            ["PUT"] = System.Net.Http.HttpMethod.Put,
            ["DELETE"] = System.Net.Http.HttpMethod.Delete,
            ["PATCH"] = System.Net.Http.HttpMethod.Patch,
            ["HEAD"] = System.Net.Http.HttpMethod.Head
        };
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        string pattern = "([A-Za-z0-9-_])";
        if (!Regex.IsMatch(Path, pattern))
        {
            yield return new ValidationResult("Path can only contain letters, digits, underscores and hyphens");
        }

        if (!_allowedMethods.TryGetValue(HttpMethod, out _))
        {
            yield return new ValidationResult("Unsupported HTTP method");
        }
    }
}