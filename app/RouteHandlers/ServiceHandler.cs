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
            int? userId = RouteHelper.GetUserId(user);
            if (userId == null) return Results.Unauthorized();
            
            var services = await dbContext.Services
                .Where(s => s.ApplicationUserId == userId)
                .Select(s => new
                {
                    s.ApplicationUserId,
                    s.ServiceId,
                    s.Name,
                    s.BaseUrl
                })
                .ToListAsync(cancellationToken);
            
            return Results.Ok(services);
        }).RequireAuthorization();

        app.MapGet("/services/{serviceId}",
            async (int serviceId, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                
                Service? service = await dbContext.Services
                    .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.ApplicationUserId == userId, cancellationToken);
                if (service == null) return Results.NotFound();

                var serviceReturn = new
                {
                    service.ApplicationUserId,
                    service.ServiceId,
                    service.Name,
                    service.BaseUrl,
                };
                
                return Results.Ok(serviceReturn);
            }).RequireAuthorization();

        app.MapPost("/services",
            async ([FromBody] ServiceDto serviceDto, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                Service service = new Service
                {
                    ApplicationUserId = (int)userId,
                    Name = serviceDto.Name,
                    BaseUrl = serviceDto.BaseUrl,
                    ServiceStatus = ServiceStatus.Unknown
                };

                var entry = dbContext.Services.Add(service);
                await dbContext.SaveChangesAsync(cancellationToken);

                var entity = entry.Entity;
                string uri = $"/services/{entity.ServiceId}";

                return Results.Created(uri, null);
            }).RequireAuthorization();

        app.MapPut("/services/{serviceId}",
            async (int serviceId, ServiceDto serviceDto, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken cancellationToken) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                
                Service? service = await dbContext.Services
                    .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.ApplicationUserId == userId, cancellationToken);
                if (service == null) return Results.NotFound();

                service.Name = serviceDto.Name;
                service.BaseUrl = serviceDto.BaseUrl;
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            }).RequireAuthorization();
        
        app.MapDelete("/services/{serviceId}",
            async (int serviceId, PulseDbContext dbContext, CancellationToken cancellationToken, ClaimsPrincipal user) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();
                
                if (serviceId <= 0) return Results.BadRequest();
                
                Service? service = await dbContext.Services
                    .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.ApplicationUserId == userId, cancellationToken);
                if (service == null) return Results.NotFound();

                dbContext.Services.Remove(service);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.NoContent();
            }).RequireAuthorization();
    }
}