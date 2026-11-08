using app.Monitoring;

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
}