using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.Monitoring;

public class ServiceStatusHandler(PulseDbContext dbContext)
{
    public async Task ConsiderServiceStatus(Service service, CancellationToken token)
    {
        var latestResults = await dbContext.Monitors
            .Where(m => m.ServiceId == service.ServiceId && m.IsEnabled)
            .Select(m => new
            {
                m.MonitorId,
                LatestOutcome = m.CheckResults
                    .OrderByDescending(r => r.CheckedAt)
                    .Select(r => (CheckOutcomeType?)r.Outcome)
                    .FirstOrDefault()
            })
            .ToListAsync(token);

        service.ServiceStatus = latestResults.Count == 0 || latestResults.Any(r => r.LatestOutcome is null)
            ? ServiceStatus.Unknown
            : latestResults.All(r => r.LatestOutcome == CheckOutcomeType.Success)
                ? ServiceStatus.Up
                : ServiceStatus.Down;
        
        await dbContext.SaveChangesAsync(token);
    }
}