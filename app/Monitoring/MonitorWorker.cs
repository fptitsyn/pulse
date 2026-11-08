namespace app.Monitoring;

public class MonitorWorker(IServiceScopeFactory scopeFactory, ILogger<MonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<DueMonitorProcessor>();
                var checkResults = await processor.ProcessDueAsync(stoppingToken);
                
                var serviceStatusHandler = scope.ServiceProvider.GetRequiredService<ServiceStatusHandler>();
                foreach (var checkResult in checkResults)
                {
                    await serviceStatusHandler.ConsiderServiceStatus(checkResult, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monitoring iteration failed");
            }
            
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}