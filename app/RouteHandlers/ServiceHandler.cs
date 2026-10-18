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
            var service = await dbContext.Services.FindAsync(id, cancellationToken);
            return Results.Ok(service);
        });

        app.MapPost("/services",
            async ([FromBody] Service service, PulseDbContext dbContext, CancellationToken cancellationToken) =>
            {
                await dbContext.Services.AddAsync(service, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Created();
        });
    }
}