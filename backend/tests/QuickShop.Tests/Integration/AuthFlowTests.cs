using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QuickShop.Api.DTOs;
using QuickShop.Tests.Infrastructure;
using Xunit;

namespace QuickShop.Tests.Integration;

[Collection(IntegrationFixture.Name)]
public class AuthFlowTests
{
    private readonly QuickShopApiFactory _factory;

    public AuthFlowTests(QuickShopApiFactory factory) => _factory = factory;

    private static string NewEmail() => $"shopper.{Guid.NewGuid():N}@test.dev";

    [Fact]
    public async Task Register_creates_customer_and_signs_in()
    {
        using var api = new ApiClient(_factory);
        var email = NewEmail();

        var response = await api.PostAnonymousAsync("/api/auth/register", new
        {
            fullName = "Test Shopper",
            email,
            password = "Shopper@123",
            confirmPassword = "Shopper@123",
            acceptTerms = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ApiClient.ReadJsonAsync(response);
        Assert.Equal(email, body.GetProperty("user").GetProperty("email").GetString());
        Assert.Contains("Customer", body.GetProperty("user").GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()));

        // Registration signs the user in via the HttpOnly cookie.
        Assert.True(api.HasAuthCookie);
        Assert.False(string.IsNullOrEmpty(api.XsrfToken));
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_with_409()
    {
        using var api = new ApiClient(_factory);
        var email = NewEmail();
        await api.RegisterAsync(email, "Shopper@123");

        using var second = new ApiClient(_factory);
        var response = await second.PostAnonymousAsync("/api/auth/register", new
        {
            fullName = "Duplicate",
            email,
            password = "Shopper@123",
            confirmPassword = "Shopper@123",
            acceptTerms = true
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("short1A", "short1A")]       // under 8 characters
    [InlineData("alllowercase1", "alllowercase1")] // no uppercase
    [InlineData("Mismatch@123", "Mismatch@456")]   // confirm != password
    public async Task Register_validates_input(string password, string confirm)
    {
        using var api = new ApiClient(_factory);
        var response = await api.PostAnonymousAsync("/api/auth/register", new
        {
            fullName = "Validation Tester",
            email = NewEmail(),
            password,
            confirmPassword = confirm,
            acceptTerms = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_401()
    {
        // Register on one client (which signs the user in), then attempt the
        // failing login from a fresh, cookie-less client — a real visitor mistyping
        // their password has no session yet. (A cookie-carrying client would get 403
        // from the CSRF middleware on the unsafe POST, which is correct behaviour.)
        using var registered = new ApiClient(_factory);
        var email = NewEmail();
        await registered.RegisterAsync(email, "Shopper@123");

        using var attempt = new ApiClient(_factory);
        var response = await attempt.PostAnonymousAsync("/api/auth/login", new { email, password = "Wrong@999" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(attempt.HasAuthCookie);
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        using var anonymous = new ApiClient(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

        using var api = new ApiClient(_factory);
        await api.LoginAsync("rahul.sharma@demo.dev", "Demo@12345");

        var me = await api.ReadAsync<AuthUserDto>(await api.GetAsync("/api/auth/me"));
        Assert.Equal("rahul.sharma@demo.dev", me!.Email);
        Assert.Contains("Customer", me.Roles);
    }

    [Fact]
    public async Task Cookie_authenticated_POST_without_csrf_header_is_403()
    {
        using var api = new ApiClient(_factory);
        await api.LoginAsync("rahul.sharma@demo.dev", "Demo@12345");

        // Simulates a cross-site form post: cookies are attached, the header is not.
        var response = await api.PostWithoutCsrfAsync("/api/cart/items", new { productId = 1, quantity = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_authenticated_POST_with_csrf_header_succeeds()
    {
        using var api = new ApiClient(_factory);
        await api.LoginAsync("rahul.sharma@demo.dev", "Demo@12345");

        var response = await api.PostAsync("/api/cart/items", new { productId = 1, quantity = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_session_and_the_csrf_token()
    {
        using var api = new ApiClient(_factory);
        await api.LoginAsync("priya.verma@demo.dev", "Demo@12345");
        var xsrfBefore = api.XsrfToken;

        var refresh = await api.PostAsync("/api/auth/refresh", new { rememberMe = true });

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.True(api.HasAuthCookie);

        // The rotation means any previously captured CSRF token is now stale.
        Assert.NotEqual(xsrfBefore, api.XsrfToken);

        // And the renewed session can still call the API.
        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_clears_the_session()
    {
        using var api = new ApiClient(_factory);
        await api.LoginAsync("arjun.iyer@demo.dev", "Demo@12345");
        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("/api/auth/me")).StatusCode);

        var logout = await api.PostAsync("/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        // The auth cookie is expired; the session is gone.
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Password_reset_flow_issues_single_use_token()
    {
        using var api = new ApiClient(_factory);
        var email = NewEmail();
        await api.RegisterAsync(email, "Original@123");

        // Mint a real reset token exactly the way the service does.
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<QuickShop.Api.Domain.ApplicationUser>>();
            var user = await users.FindByEmailAsync(email)
                ?? throw new InvalidOperationException("Seeded user missing.");
            token = await users.GeneratePasswordResetTokenAsync(user);
        }

        // The reset POST is made from a cookie-less client — like a user clicking
        // the emailed link in a browser with no active session.
        using var resetClient = new ApiClient(_factory);
        var reset = await resetClient.PostAnonymousAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword = "Reset@12345",
            confirmNewPassword = "Reset@12345"
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // New password works...
        using var fresh = new ApiClient(_factory);
        var login = await fresh.PostAnonymousAsync("/api/auth/login", new { email, password = "Reset@12345" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // ...and the token is single-use: reusing it must fail.
        using var reuseClient = new ApiClient(_factory);
        var reuse = await reuseClient.PostAnonymousAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword = "Third@12345",
            confirmNewPassword = "Third@12345"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }
}
