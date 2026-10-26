using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace QuickShop.Api.Security;

public static class SecurityExtensions
{
    /// <summary>
    /// JWT bearer authentication that accepts the token from either the Authorization
    /// header (Bearer) or the HttpOnly auth cookie, plus role-based policies and the
    /// cookie-based CSRF double-submit check for unsafe methods.
    /// </summary>
    public static IServiceCollection AddQuickShopSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<random string, 64+ chars>\" --project backend/src/QuickShop.Api (see README).");

        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters long. See README.");

        var issuer = configuration["Jwt:Issuer"] ?? "QuickShop";
        var audience = configuration["Jwt:Audience"] ?? "QuickShop.Client";

        // AddIdentity sets DefaultAuthenticateScheme/DefaultChallengeScheme to the
        // Identity.Application cookie (which redirects to /Account/Login). This API
        // never uses those cookie schemes, so override every default to point at the
        // single JWT scheme (which reads the token from header or HttpOnly cookie).
        // This registration runs after AddIdentity, so these values win.
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    // Prefer the Authorization header; otherwise fall back to the HttpOnly cookie.
                    OnMessageReceived = context =>
                    {
                        var header = context.Request.Headers.Authorization.FirstOrDefault();
                        if (!string.IsNullOrEmpty(header) && header.StartsWith(AuthConstants.BearerPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = header[AuthConstants.BearerPrefix.Length..];
                            return Task.CompletedTask;
                        }

                        if (context.Request.Cookies.TryGetValue(AuthConstants.AuthCookieName, out var cookieToken)
                            && !string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                            context.HttpContext.Items[AuthConstants.AuthViaCookieKey] = true;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.Customer, policy => policy.RequireRole("Customer", "Admin"));
            options.AddPolicy(Policies.Admin, policy => policy.RequireRole("Admin"));
        });

        return services;
    }
}

/// <summary>
/// CSRF protection for cookie-authenticated state-changing requests using the
/// double-submit pattern: the readable XSRF cookie must be echoed in the
/// X-XSRF-TOKEN header. Bearer-header requests are not vulnerable and are exempt.
/// </summary>
public sealed class CsrfMiddleware
{
    private static readonly string[] UnsafeMethods = { "POST", "PUT", "PATCH", "DELETE" };

    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isUnsafe = UnsafeMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase);
        var viaCookie = context.Items.ContainsKey(AuthConstants.AuthViaCookieKey);
        var authenticated = context.User.Identity?.IsAuthenticated == true;

        if (isUnsafe && viaCookie && authenticated)
        {
            context.Request.Cookies.TryGetValue(AuthConstants.CsrfCookieName, out var cookieToken);
            var headerToken = context.Request.Headers[AuthConstants.CsrfHeaderName].FirstOrDefault();

            var valid = !string.IsNullOrEmpty(cookieToken)
                        && !string.IsNullOrEmpty(headerToken)
                        && string.Equals(cookieToken, headerToken, StringComparison.Ordinal);

            if (!valid)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    "{\"type\":\"https://httpstatuses.com/403\",\"title\":\"CSRF token missing or invalid\",\"status\":403," +
                    "\"detail\":\"Include the X-XSRF-TOKEN header from the quickshop_xsrf cookie on state-changing requests.\"}");
                return;
            }
        }

        await _next(context);
    }
}
