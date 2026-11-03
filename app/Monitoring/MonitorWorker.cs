using app.Models;
using Microsoft.EntityFrameworkCore;
using Monitor = app.Models.Monitor;

namespace app.Monitoring;

public class MonitorWorker(PulseDbContext dbContext, ILogger<MonitorWorker> logger, HttpMonitorChecker httpChecker,
    CheckResultWriter resultWriter)
    : BackgroundService
{
    private ILogger<MonitorWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var dueMonitors = await dbContext.Monitors.Select(m => new
                {
                    m.MonitorId,
                    m.ServiceId,
                    m.IsEnabled,
                    m.Path,
                    m.IntervalSeconds,
                    m.NextCheckAt,
                    HttpMethod = m.HttpMethod.Method
                }
            ).Where(m => m.IsEnabled && m.NextCheckAt >= DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            foreach (var monitor in dueMonitors)
            {
                Service? service = await dbContext.Services.FindAsync([monitor.ServiceId], stoppingToken);
                if (service == null)
                {
                    _logger.LogDebug("Service for monitor with id {MonitorId} not found", monitor.MonitorId);
                    continue;
                }
                
                Uri path = new Uri(monitor.Path.TrimStart('/'), UriKind.Relative);
                Uri baseUri = new Uri(service.BaseUrl.TrimEnd('/'));
                Uri finalUri = new Uri(path, baseUri);

                HttpMethod method = HttpMethod.Parse(monitor.HttpMethod);
                HttpCheckOutcome checkOutcome = await httpChecker.SendRequestAsync(finalUri, method, stoppingToken);

                resultWriter.WriteCheckResult(monitor.MonitorId, checkOutcome);

                DateTime newCheck = DateTime.UtcNow + TimeSpan.FromSeconds(monitor.IntervalSeconds);
                dbContext.Monitors.Add(new Monitor
                {
                    ServiceId = monitor.ServiceId,
                    MonitorId = monitor.MonitorId,
                    HttpMethod = HttpMethod.Parse(monitor.HttpMethod),
                    IntervalSeconds = monitor.IntervalSeconds,
                    IsEnabled = true,
                    NextCheckAt = newCheck,
                    Path = monitor.Path
                });

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}