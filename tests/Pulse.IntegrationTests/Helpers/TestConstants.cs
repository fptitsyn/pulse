using app.Models;

namespace Pulse.IntegrationTests.Helpers;

public static class TestConstants
{
    public const string TestPassword = "TestPassword123!";

    public static readonly ServiceDto TestService = new()
    {
        Name = "testName",
        BaseUrl = "http://test.com"
    };
    
    public static readonly MonitorDto TestMonitor = new()
    {
        Path = "testPath",
        IntervalSeconds = 3,
        HttpMethod = "HEAD"
    };
}