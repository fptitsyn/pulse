namespace app.Monitoring;

public static class CustomUriBuilder
{
    public static Uri? Build(string baseUrl, string path)
    {
        if (Uri.TryCreate($"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}", UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.HostNameType != UriHostNameType.Dns)
        {
            return uri;
        }

        return null;
    }
}