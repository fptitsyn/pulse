using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.Monitoring;

public class MonitorWorker(IServiceScopeFactory scopeFactory, ILogger<MonitorWorker> logger) 
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            
            try
            {
                var processor = scope.ServiceProvider.GetRequiredService<DueMonitorProcessor>();
                await processor.ProcessDueAsync(stoppingToken);
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
                var serviceStatusHandler = scope.ServiceProvider.GetRequiredService<ServiceStatusHandler>();
                var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
                var services = await dbContext.Services.ToListAsync(stoppingToken);

                foreach (var service in services)
                {
                    await serviceStatusHandler.ConsiderServiceStatus(service, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update service status");
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