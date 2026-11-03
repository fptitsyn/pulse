using app.Models;

namespace app.Monitoring;

public class CheckResultWriter
{
    private readonly PulseDbContext _dbContext;

    public CheckResultWriter(PulseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int WriteCheckResult(int monitorId, HttpCheckOutcome checkOutcome)
    {
        CheckResult checkResult = new CheckResult
        {
            MonitorId = monitorId,
            CheckedAt = checkOutcome.CheckedAt,
            HttpStatusCode = checkOutcome.HttpStatusCode,
            Outcome = checkOutcome.Outcome,
            DurationMs = checkOutcome.DurationMs
        };

        var entry = _dbContext.CheckResults.Add(checkResult);
        
        return entry.Entity.CheckResultId;
    }
}