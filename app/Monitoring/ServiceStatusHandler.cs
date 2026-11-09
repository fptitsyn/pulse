using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.Monitoring;

public class ServiceStatusHandler(PulseDbContext dbContext)
{
    public async Task ConsiderServiceStatus(Service service, CancellationToken token)
    {
        var monitors = await dbContext.Monitors
            .Include(m => m.CheckResults)
            .Where(m => m.ServiceId == service.ServiceId && m.IsEnabled && m.CheckResults.Count > 0)
            .ToListAsync(token);

        if (monitors.Count == 0) return;

        List<CheckResult> latestCheckResults = new();
        foreach (var monitor in monitors)
        {
            var latestForEachMonitor = monitor.CheckResults.OrderBy(ch => ch.CheckedAt).First();
            latestCheckResults.Add(latestForEachMonitor);
        }

        service.ServiceStatus = latestCheckResults.Any(ch => ch.Outcome is HttpMonitorChecker.Success or HttpMonitorChecker.HttpError) ?
            ServiceStatus.Up : ServiceStatus.Down;
        
        await dbContext.SaveChangesAsync(token);
    }
}