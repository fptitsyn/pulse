using System.Security.Claims;
using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.RouteHandlers;

public static class CheckResultRouteHandler
{
    private const string BaseUrl = "/services/{serviceId}/monitors/{monitorId}/check-results/";
    
    public static void RegisterCheckResultRoutes(this WebApplication app)
    {
        app.MapGet("/check-results/",
            async (PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHandlerHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();
                
                var checkResults = await dbContext.CheckResults
                    .Where(ch => ch.Monitor.Service.ApplicationUserId == userId)
                    .Select(ch => new
                    {
                        ch.Monitor.Path,
                        ch.CheckedAt,
                        ch.DurationMs,
                        ch.Outcome,
                        ch.HttpStatusCode
                    })
                    .ToListAsync(token);

                return Results.Ok(checkResults);
            }).RequireAuthorization();

        app.MapGet(BaseUrl,
            async (int serviceId, int monitorId, PulseDbContext dbContext, ClaimsPrincipal user, CancellationToken token) =>
            {
                int? userId = RouteHandlerHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0) return Results.BadRequest();
                if (monitorId <= 0) return Results.BadRequest();
            
                bool serviceExists = await dbContext.Services.AnyAsync(s => s.ServiceId == serviceId &&
                    s.ApplicationUserId == userId, token);
                if (!serviceExists) return Results.NotFound();
                
                bool monitorExists = await dbContext.Monitors.AnyAsync(m => m.MonitorId == monitorId 
                    && m.ServiceId == serviceId && m.Service.ApplicationUserId == userId, token);
                if (!monitorExists) return Results.NotFound();
                
                var checkResults = await dbContext.CheckResults
                    .Where(ch => ch.MonitorId == monitorId &&
                                 ch.Monitor.ServiceId == serviceId && 
                                 ch.Monitor.Service.ApplicationUserId == userId)
                    .Select(ch => new
                    {
                        ch.Monitor.Path,
                        ch.CheckedAt,
                        ch.DurationMs,
                        ch.Outcome,
                        ch.HttpStatusCode
                    })
                    .ToListAsync(token);

                return Results.Ok(checkResults);
            }).RequireAuthorization();
        
        app.MapGet(BaseUrl + "{checkResultId}",
            async (int serviceId, int monitorId, int checkResultId, PulseDbContext dbContext, ClaimsPrincipal user,
                CancellationToken token) =>
            {
                int? userId = RouteHandlerHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0 || monitorId <= 0 || checkResultId <= 0) return Results.BadRequest();

                var checkResult = await dbContext.CheckResults
                    .Include(ch => ch.Monitor)
                    .FirstOrDefaultAsync(ch =>
                    ch.Monitor.Service.ApplicationUserId == userId &&
                    ch.Monitor.ServiceId == serviceId &&
                    ch.MonitorId == monitorId && 
                    ch.CheckResultId == checkResultId, token);
                if (checkResult == null) return Results.NotFound();

                var checkResultReturn = new
                {
                    checkResult.Monitor.Path,
                    checkResult.CheckedAt,
                    checkResult.DurationMs,
                    checkResult.Outcome,
                    checkResult.HttpStatusCode
                };
                
                return Results.Ok(checkResultReturn);
            }).RequireAuthorization();
        
        app.MapDelete(BaseUrl + "{checkResultId}",
            async (int serviceId, int monitorId, int checkResultId, PulseDbContext dbContext, ClaimsPrincipal user,
                CancellationToken token) =>
            {
                int? userId = RouteHandlerHelper.GetUserId(user);
                if (userId == null) return Results.Unauthorized();

                if (serviceId <= 0 || monitorId <= 0 || checkResultId <= 0) return Results.BadRequest();

                var checkResult = await dbContext.CheckResults
                    .Include(ch => ch.Monitor)
                    .FirstOrDefaultAsync(ch =>
                        ch.Monitor.Service.ApplicationUserId == userId &&
                        ch.Monitor.ServiceId == serviceId &&
                        ch.MonitorId == monitorId && 
                        ch.CheckResultId == checkResultId, token);
                if (checkResult == null) return Results.NotFound();

                dbContext.CheckResults.Remove(checkResult);
                await dbContext.SaveChangesAsync(token);

                return Results.NoContent();
            }).RequireAuthorization();
    }
}