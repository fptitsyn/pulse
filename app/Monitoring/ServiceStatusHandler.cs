using app.Models;

namespace app.Monitoring;

public class ServiceStatusHandler(PulseDbContext dbContext)
{
    public async Task ConsiderServiceStatus(CheckResult checkResult, CancellationToken token)
    {
        // var monitor = await dbContext.Monitors.FindAsync([checkResult.MonitorId], token);
        // var service = await dbContext.Services.FindAsync([monitor.ServiceId], token);
        var service = await dbContext.Services.FindAsync([checkResult.Monitor.ServiceId], token);
        
        service.ServiceStatus = checkResult.Outcome switch
        {
            HttpMonitorChecker.Success or HttpMonitorChecker.HttpError => ServiceStatus.Up,
            HttpMonitorChecker.NetworkError or HttpMonitorChecker.RequestTimeout => ServiceStatus.Down,
            _ => ServiceStatus.Unknown
        };

        await dbContext.SaveChangesAsync(token);
    }
}