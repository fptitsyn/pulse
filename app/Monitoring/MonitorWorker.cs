using app.Models;

namespace app.Monitoring;

public class MonitorWorker(IServiceScopeFactory scopeFactory, ILogger<DueMonitorProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
                await using var scope = scopeFactory.CreateAsyncScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
                var httpChecker = scope.ServiceProvider.GetRequiredService<HttpMonitorChecker>();
                var resultWriter = scope.ServiceProvider.GetRequiredService<CheckResultWriter>();

                var processor = new DueMonitorProcessor(dbContext, httpChecker, resultWriter, logger);

                await processor.ProcessDueAsync(stoppingToken);
                
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
        }
    }
}