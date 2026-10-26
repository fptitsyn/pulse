using System.Security.Claims;
using app.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitor = app.Models.Monitor;

namespace app.RouteHandlers;

public static class MonitorHandler
{
    private const string BaseUrl = "/services/{serviceId}/monitors/";

    public static void RegisterMonitorRoutes(this WebApplication app)
    {
        app.MapGet(BaseUrl,
            async (int serviceId, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                
                bool serviceExists = await dbContext.Services.AnyAsync(s => s.ServiceId == serviceId &&
                                                                            s.ApplicationUserId == userId, token);
                if (!serviceExists) return Results.NotFound();
                
                var monitors = await dbContext.Monitors
                    .Where(m => m.ServiceId == serviceId &&
                                m.Service.ApplicationUserId == userId)
                    .Select(m => new
                    {
                        m.MonitorId,
                        m.ServiceId,
                        m.Path,
                        m.IntervalSeconds,
                        m.HttpMethodId
                    })
                    .ToListAsync(token);

                return Results.Ok(monitors);
            }).RequireAuthorization();

        app.MapGet(BaseUrl + "{monitorId}",
            async (int serviceId, int monitorId, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                if (monitorId <= 0) return Results.BadRequest();
                
                var monitor = await dbContext.Monitors.FirstOrDefaultAsync(m => m.Service.ApplicationUserId == userId &&
                    m.ServiceId == serviceId && m.MonitorId == monitorId, token);
                if (monitor == null) return Results.NotFound();

                var monitorReturn = new
                {
                    monitor.ServiceId,
                    monitor.MonitorId,
                    monitor.Path,
                    monitor.IntervalSeconds,
                    monitor.HttpMethodId
                };
                
                return Results.Ok(monitorReturn);
            }).RequireAuthorization();

        app.MapPost(BaseUrl,
            async ([FromBody] MonitorDto monitorDto, int serviceId, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                
                var service = await dbContext.Services.FirstOrDefaultAsync(s => 
                        s.ServiceId == serviceId && s.ApplicationUserId == userId, token);
                if (service == null) return Results.NotFound();
                
                Monitor monitor = new Monitor
                {
                    ServiceId = serviceId,
                    Path = monitorDto.Path,
                    IntervalSeconds = monitorDto.IntervalSeconds,
                    HttpMethodId = monitorDto.HttpMethodId
                };

                var entry = dbContext.Monitors.Add(monitor);
                await dbContext.SaveChangesAsync(token);

                string uri = $"/services/{serviceId}/monitors/{entry.Entity.MonitorId}";

                return Results.Created(uri, null);
            }).RequireAuthorization();

        app.MapPut(BaseUrl + "{monitorId}",
            async ([FromBody] MonitorDto monitorDto, int serviceId, int monitorId, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                if (monitorId <= 0) return Results.BadRequest();
                
                var monitor = await dbContext.Monitors.FirstOrDefaultAsync(m => m.Service.ApplicationUserId == userId &&
                    m.ServiceId == serviceId && m.MonitorId == monitorId, token);
                if (monitor == null) return Results.NotFound();

                monitor.Path = monitorDto.Path;
                monitor.IntervalSeconds = monitorDto.IntervalSeconds;
                monitor.HttpMethodId = monitorDto.HttpMethodId;

                await dbContext.SaveChangesAsync(token);

                return Results.NoContent();
            }).RequireAuthorization();

        app.MapDelete(BaseUrl + "{monitorId}",
            async (int serviceId, int monitorId, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                if (monitorId <= 0) return Results.BadRequest();
                
                var monitor = await dbContext.Monitors
                    .FirstOrDefaultAsync(m => m.Service.ApplicationUserId == userId &&
                                              m.ServiceId == serviceId && m.MonitorId == monitorId, token);
                if (monitor == null) return Results.NotFound();

                dbContext.Monitors.Remove(monitor);
                await dbContext.SaveChangesAsync(token);

                return Results.NoContent();
            }).RequireAuthorization();
    }
}