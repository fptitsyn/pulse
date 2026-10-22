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
        });

        app.MapGet("/services/{id}", async (int id, PulseDbContext dbContext, CancellationToken cancellationToken) =>
        {
            Service? service = await dbContext.Services.FindAsync(id, cancellationToken);
            if (service == null)
            {
                return Results.NotFound();
            }
            
            return Results.Ok(service);
        });

        app.MapPost("/services",
            async ([FromBody] ServiceDto serviceDto, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                string? userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Results.Unauthorized();
                }
                
                Service service = new Service
                {
                    ApplicationUserId = int.Parse(userId),
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
    }
}