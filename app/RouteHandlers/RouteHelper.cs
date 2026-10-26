using System.Security.Claims;

namespace app.RouteHandlers;

public static class RouteHelper
{
    public static int? GetUserId(ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;

        return int.Parse(userId);
    }
}
