using app.Models;

namespace app.Monitoring;

public class CheckResultWriter(PulseDbContext dbContext)
{
    public CheckResult AddCheckResult(int monitorId, HttpCheckOutcome checkOutcome)
    {
        var entityEntry = dbContext.CheckResults.Add(new CheckResult
        {
            MonitorId = monitorId,
            CheckedAt = checkOutcome.CheckedAt,
            HttpStatusCode = checkOutcome.HttpStatusCode,
            Outcome = checkOutcome.Outcome,
            DurationMs = checkOutcome.DurationMs
        });

        return entityEntry.Entity;
    }
}