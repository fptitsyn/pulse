using System.Security.Claims;
using app.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace app.RouteHandlers;

public static class ServiceHandler
{
    public static void RegisterServiceRoutes(this WebApplication app)
    {
        app.MapGet("/services", async (CancellationToken cancellationToken, PulseDbContext dbContext) =>
        {
            var services = await dbContext.Services.ToListAsync(cancellationToken);
            return Results.Ok(services);
        }).RequireAuthorization();

        app.MapGet("/services/{id}",
            async (int id, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int userId = GetUserId(user);
                if (userId == -1) return Results.Unauthorized();

                Service? service = await dbContext.Services.FindAsync([id], cancellationToken);
                if (service == null) return Results.NotFound();

                if (service.ApplicationUserId != userId) return Results.Forbid();

                return Results.Ok(service);
            }).RequireAuthorization();

        app.MapPost("/services",
            async ([FromBody] ServiceDto serviceDto, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int userId = GetUserId(user);
                if (userId == -1) return Results.Unauthorized();

                Service service = new Service
                {
                    ApplicationUserId = userId,
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
                int userId = GetUserId(user);
                if (userId == -1) return Results.Unauthorized();

                Service? service = await dbContext.Services.FindAsync([id], cancellationToken);
                if (service == null) return Results.NotFound();

                if (service.ApplicationUserId != userId) return Results.Forbid();
                
                service.Name = serviceDto.Name;
                service.BaseUrl = serviceDto.BaseUrl;
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            }).RequireAuthorization();
        
        app.MapDelete("/services/{id}",
            async (int id, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int userId = GetUserId(user);
                if (userId == -1) return Results.Unauthorized();
                
                Service? service = await dbContext.Services.FindAsync([id], cancellationToken);
                if (service == null) return Results.NotFound();

                if (service.ApplicationUserId != userId) return Results.Forbid();
                
                dbContext.Services.Remove(service);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            }).RequireAuthorization();
    }

    private static int GetUserId(ClaimsPrincipal user)
    {
        string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return -1;

        return int.Parse(userId);
    }
}