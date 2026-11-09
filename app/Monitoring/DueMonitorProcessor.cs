using app.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Security.AntiSSRF;

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
            if (monitor.HttpMethod != HttpMethod.Get && monitor.HttpMethod != HttpMethod.Head)
            {
                logger.LogWarning("Monitor #{MonitorId} has an unsupported HTTP method and will be disabled", monitor.MonitorId);
                monitor.IsEnabled = false;
                await dbContext.SaveChangesAsync(token);
                continue;
            }

            Uri? uri = CustomUriBuilder.Build(monitor.Service.BaseUrl, monitor.Path);

            if (uri is null)
            {
                logger.LogWarning("Monitor #{MonitorId} has an invalid URI and will be disabled", monitor.MonitorId);
                monitor.IsEnabled = false;
                await dbContext.SaveChangesAsync(token);
                continue;
            }

            HttpCheckOutcome checkOutcome;

            try
            {
                checkOutcome = await httpChecker.SendRequestAsync(uri, monitor.HttpMethod, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (AntiSSRFException ex)
            {
                logger.LogError(ex, "Invalid configuration for security reasons for monitor #{MonitorId}." +
                                    " It will be disabled", monitor.MonitorId);
                monitor.IsEnabled = false;
                await dbContext.SaveChangesAsync(token);
                continue;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected checking failure for monitor {MonitorId}", monitor.MonitorId);
                monitor.NextCheckAt = DateTime.UtcNow.AddSeconds(30 + monitor.IntervalSeconds);
                await dbContext.SaveChangesAsync(token);
                continue;
            }

            resultWriter.AddCheckResult(monitor.MonitorId, checkOutcome);
            monitor.NextCheckAt = checkOutcome.CheckedAt.AddSeconds(monitor.IntervalSeconds);
            
            await dbContext.SaveChangesAsync(token);
        }
    }
}