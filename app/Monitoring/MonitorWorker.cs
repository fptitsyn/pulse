using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.Monitoring;

public class MonitorWorker(IServiceScopeFactory scopeFactory, ILogger<MonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MonitorWorker started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try {
                await using var scope = scopeFactory.CreateAsyncScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
                var httpChecker = scope.ServiceProvider.GetRequiredService<HttpMonitorChecker>();
                var resultWriter = scope.ServiceProvider.GetRequiredService<CheckResultWriter>();

                var now = DateTime.UtcNow;

                var dueMonitors = await dbContext.Monitors
                    .Include(m => m.Service)
                    .Where(m => m.IsEnabled && m.NextCheckAt <= now)
                    .OrderBy(m => m.NextCheckAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                logger.LogInformation(
                    "Found {Count} due monitors",
                    dueMonitors.Count);
                
                foreach (var monitor in dueMonitors)
                {
                    Uri uri = new Uri($"{monitor.Service.BaseUrl.TrimEnd('/')}/{monitor.Path.TrimStart('/')}",
                        UriKind.Absolute);
                    var checkOutcome = await httpChecker.SendRequestAsync(uri, monitor.HttpMethod, stoppingToken);

                    resultWriter.AddCheckResult(monitor.MonitorId, checkOutcome);

                    monitor.NextCheckAt = checkOutcome.CheckedAt.AddSeconds(monitor.IntervalSeconds);

                    await dbContext.SaveChangesAsync(stoppingToken);
                    logger.LogDebug("Uri: {UriOriginalString}, outcome: {CheckOutcomeOutcome}",
                        uri.OriginalString, checkOutcome.Outcome);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monitoring iteration failed");
            }
            
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}