using System.Security.Claims;

namespace app.RouteHandlers;

public static class RouteHandlerHelper
{
    public static int? GetUserId(ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;

        return int.Parse(userId);
    }
}
