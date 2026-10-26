using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QuickShop.Tests.Infrastructure;

/// <summary>
/// Browser-like API client for integration tests: persists cookies (the HttpOnly
/// auth cookie), echoes the readable XSRF cookie in the X-XSRF-TOKEN header on
/// unsafe methods (the API's double-submit CSRF rule), and always sends a unique
/// X-Cart-Id like the storefront does. The XSRF value is tracked by watching
/// Set-Cookie headers on every response.
/// </summary>
public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

    public ApiClient(QuickShopApiFactory factory)
    {
        _http = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        CartId = Guid.NewGuid().ToString();
        _http.DefaultRequestHeaders.Add("X-Cart-Id", CartId);
    }

    public string CartId { get; }

    /// <summary>Latest value of the readable quickshop_xsrf cookie, as observed from responses.</summary>
    public string? XsrfToken => _cookies.TryGetValue("quickshop_xsrf", out var value) ? value : null;

    /// <summary>True once a quickshop_auth cookie has been issued and not cleared.</summary>
    public bool HasAuthCookie => _cookies.ContainsKey("quickshop_auth");

    public async Task<HttpResponseMessage> GetAsync(string url)
        => Track(await _http.GetAsync(url));

    public async Task<HttpResponseMessage> PostAnonymousAsync<TBody>(string url, TBody body)
        => Track(await _http.PostAsJsonAsync(url, body));

    /// <summary>Cookie-authenticated unsafe request with the CSRF header echoed.</summary>
    public async Task<HttpResponseMessage> PostAsync<TBody>(string url, TBody body)
        => Track(await SendWithCsrfAsync(HttpMethod.Post, url, JsonContent.Create(body)));

    public async Task<HttpResponseMessage> PutAsync<TBody>(string url, TBody body)
        => Track(await SendWithCsrfAsync(HttpMethod.Put, url, JsonContent.Create(body)));

    public async Task<HttpResponseMessage> DeleteAsync(string url)
        => Track(await SendWithCsrfAsync(HttpMethod.Delete, url, content: null));

    /// <summary>POST without the CSRF header — used to prove the middleware blocks it.</summary>
    public async Task<HttpResponseMessage> PostWithoutCsrfAsync<TBody>(string url, TBody body)
        => Track(await _http.PostAsJsonAsync(url, body));

    private async Task<HttpResponseMessage> SendWithCsrfAsync(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        if (XsrfToken is not null)
            request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", XsrfToken);
        return await _http.SendAsync(request);
    }

    /// <summary>Records Set-Cookie headers so XSRF rotation stays visible to the test.</summary>
    private HttpResponseMessage Track(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                var pair = cookie.Split(';', 2)[0];
                var separator = pair.IndexOf('=');
                if (separator <= 0)
                    continue;

                var name = pair[..separator].Trim();
                var value = pair[(separator + 1)..].Trim();

                // An empty value with expiry in the past means the cookie was cleared.
                if (value.Length == 0)
                    _cookies.Remove(name);
                else
                    _cookies[name] = value;
            }
        }

        return response;
    }

    // ---- common flows -------------------------------------------------------

    public async Task RegisterAsync(string email, string password, string fullName = "Test Shopper")
    {
        var response = await PostAnonymousAsync("/api/auth/register", new
        {
            fullName,
            email,
            password,
            confirmPassword = password,
            acceptTerms = true
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task LoginAsync(string email, string password, bool rememberMe = true)
    {
        var response = await PostAnonymousAsync("/api/auth/login", new { email, password, rememberMe });
        response.EnsureSuccessStatusCode();
    }

    public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<T>();

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    public void Dispose() => _http.Dispose();
}
