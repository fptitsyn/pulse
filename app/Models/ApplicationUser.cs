using Microsoft.AspNetCore.Identity;

namespace app.Models;

public class ApplicationUser : IdentityUser<int>
{
    public ICollection<Service> Services { get; set; } = new List<Service>();
}