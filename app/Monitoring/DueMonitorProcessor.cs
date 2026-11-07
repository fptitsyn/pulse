using System.Diagnostics;
using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.Monitoring;

public class DueMonitorProcessor(
    PulseDbContext dbContext,
    HttpMonitorChecker httpChecker,
    CheckResultWriter resultWriter,
    ILogger<DueMonitorProcessor> logger)
{
    public async Task ProcessDueAsync(CancellationToken token)
    {
        var now = DateTime.UtcNow;

        var dueMonitors = await dbContext.Monitors
            .Include(m => m.Service)
            .Where(m => m.IsEnabled && m.NextCheckAt <= now)
            .OrderBy(m => m.NextCheckAt)
            .Take(50)
            .ToListAsync(token);

        logger.LogDebug("Found {Count} due monitors", dueMonitors.Count);
        
        foreach (var monitor in dueMonitors)
        {
            try
            {
                if (monitor.HttpMethod != HttpMethod.Get &&
                    monitor.HttpMethod != HttpMethod.Head)
                {
                    logger.LogWarning(
                        "Monitor {MonitorId} uses an unsupported HTTP method",
                        monitor.MonitorId);

                    continue;
                }

                Uri? uri = CustomUriBuilder.Build(monitor.Service.BaseUrl, monitor.Path);
                if (uri == null)
                {
                    var ex = new ArgumentException("Invalid URI");
                    logger.LogError(ex, "Invalid URI for Monitor #{monitorId}", monitor.MonitorId);
                    throw ex;
                }
                
                var checkOutcome = await httpChecker.SendRequestAsync(uri, monitor.HttpMethod, token);

                resultWriter.AddCheckResult(monitor.MonitorId, checkOutcome);

                monitor.NextCheckAt = checkOutcome.CheckedAt.AddSeconds(monitor.IntervalSeconds);

                await dbContext.SaveChangesAsync(token);
                logger.LogDebug("Uri: {UriOriginalString}, outcome: {CheckOutcomeOutcome}",
                    uri.OriginalString, checkOutcome.Outcome);
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process monitor {MonitorId}", monitor.MonitorId);
                // if failCount > 10 then disable monitor
            }
        }
    }
}