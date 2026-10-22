using System.Security.Claims;
using app.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace app.RouteHandlers;

public static class ServiceHandler
{
    public static void RegisterServiceRoutes(this WebApplication app)
    {
        app.MapGet("/services", async (CancellationToken cancellationToken, PulseDbContext dbContext, ClaimsPrincipal user) =>
        {
            int? userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();
            
            var services = await dbContext.Services
                .Where(s => s.ApplicationUserId == userId)
                .ToListAsync(cancellationToken);
            
            return Results.Ok(services);
        }).RequireAuthorization();

        app.MapGet("/services/{id}",
            async (int id, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int? userId = GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                Service? service = await dbContext.Services
                    .FirstOrDefaultAsync(s => s.ServiceId == id && s.ApplicationUserId == userId, cancellationToken);
                if (service == null) return Results.NotFound();

                return Results.Ok(service);
            }).RequireAuthorization();

        app.MapPost("/services",
            async ([FromBody] ServiceDto serviceDto, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int? userId = GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                Service service = new Service
                {
                    ApplicationUserId = (int)userId,
                    Name = serviceDto.Name,
                    BaseUrl = serviceDto.BaseUrl,
                    ServiceStatusId = ServiceStatusIds.Unknown
                };

                var entry = dbContext.Services.Add(service);
                await dbContext.SaveChangesAsync(cancellationToken);

                var entity = entry.Entity;
                string uri = $"/services/{entity.ServiceId}";

                return Results.Created(uri, null);
            }).RequireAuthorization();

        app.MapPut("/services/{id}",
            async (int id, ServiceDto serviceDto, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken cancellationToken) =>
            {
                int? userId = GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                Service? service = await dbContext.Services
                    .FirstOrDefaultAsync(s => s.ServiceId == id && s.ApplicationUserId == userId, cancellationToken);
                if (service == null) return Results.NotFound();

                service.Name = serviceDto.Name;
                service.BaseUrl = serviceDto.BaseUrl;
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            }).RequireAuthorization();
        
        app.MapDelete("/services/{id}",
            async (int id, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int? userId = GetUserId(user);
                if (userId == null) return Results.Unauthorized();
                
                Service? service = await dbContext.Services
                    .FirstOrDefaultAsync(s => s.ServiceId == id && s.ApplicationUserId == userId, cancellationToken);
                if (service == null) return Results.NotFound();

                dbContext.Services.Remove(service);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            }).RequireAuthorization();
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;

        return int.Parse(userId);
    }
}