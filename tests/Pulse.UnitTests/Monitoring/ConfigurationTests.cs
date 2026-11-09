using app.Monitoring;
using Microsoft.Security.AntiSSRF;

namespace Pulse.UnitTests.Monitoring;

public class ConfigurationTests
{
    [Theory]
    [InlineData("https://base/", "/path")]
    [InlineData("https://base", "/path")]
    [InlineData("https://base/", "path")]
    [InlineData("https://base", "path")]
    public void BaseUrlAndPath_CombinedSuccessfully(string baseUrl, string path)
    {
        Uri validUri = new Uri("https://base/path");
        Uri? uri = CustomUriBuilder.Build(baseUrl, path);
        Assert.Equal(validUri, uri);
    }
    
    [Fact]
    public async Task Monitor_WithInternalDestination_IsRejected()
    {
        var policy = new AntiSSRFPolicy(PolicyConfigOptions.ExternalOnlyLatest);
        using var httpClient = new HttpClient(policy.GetHandler());

        await Assert.ThrowsAsync<AntiSSRFException>(
            () => httpClient.GetAsync("https://localhost/health", TestContext.Current.CancellationToken));
    } 
}