using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pulse.IntegrationTests.Helpers;

public static class TestAuthentication
{
    public async static Task RegisterUserAsync(HttpClient client, string email, string password)
    {
        var request = new
        {
            email,
            password
        };

        using var response = await client.PostAsJsonAsync(
            "/register",
            request,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var request = new
        {
            email,
            password
        };

        using var response = await client.PostAsJsonAsync("/login?useCookies=false",
            request,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken);
        string token = json.RootElement
            .GetProperty("accessToken")
            .GetString();

        if (token == null)
        {
            throw new HttpIOException(HttpRequestError.UserAuthenticationError, "Login Bearer token is null");
        }
        
        return token;
    }

    public static async Task<HttpClient> RegisterAndLoginUserAsync(HttpClient client, string email, string password)
    {
        await RegisterUserAsync(client, email, password);
        string token = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}