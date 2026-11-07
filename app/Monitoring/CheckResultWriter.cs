using app.Models;

namespace app.Monitoring;

public class CheckResultWriter(PulseDbContext dbContext)
{
    public void AddCheckResult(int monitorId, HttpCheckOutcome checkOutcome)
    {
        dbContext.CheckResults.Add(new CheckResult
        {
            MonitorId = monitorId,
            CheckedAt = checkOutcome.CheckedAt,
            HttpStatusCode = checkOutcome.HttpStatusCode,
            Outcome = checkOutcome.Outcome,
            DurationMs = checkOutcome.DurationMs
        });
    }
}