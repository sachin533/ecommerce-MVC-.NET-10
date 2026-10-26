using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QuickShop.Api.Domain;

namespace QuickShop.Api.Security;

public record IssuedToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    IssuedToken CreateToken(ApplicationUser user, IReadOnlyList<string> roles);
}

/// <summary>Creates signed JWTs. The signing key comes from configuration/user-secrets, never source.</summary>
public sealed class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly TimeSpan _lifetime;

    public TokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured. Run: dotnet user-secrets set \"Jwt:Key\" \"<random 64+ chars>\" (see README).");

        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters. See README for setup instructions.");

        _issuer = configuration["Jwt:Issuer"] ?? "QuickShop";
        _audience = configuration["Jwt:Audience"] ?? "QuickShop.Client";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var minutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 120;
        _lifetime = TimeSpan.FromMinutes(minutes);
    }

    public IssuedToken CreateToken(ApplicationUser user, IReadOnlyList<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(_lifetime);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return new IssuedToken(handler.WriteToken(token), expires);
    }
}

/// <summary>Issues/clears the auth + CSRF cookies. Token stays out of JS-readable storage.</summary>
public interface IAuthCookieService
{
    void Attach(HttpResponse response, IssuedToken token, bool persistent);
    void Clear(HttpResponse response);
}

public sealed class AuthCookieService : IAuthCookieService
{
    private readonly IConfiguration _configuration;

    public AuthCookieService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private bool SecureCookies => _configuration.GetValue("Auth:SecureCookies", false);

    public void Attach(HttpResponse response, IssuedToken token, bool persistent)
    {
        var authOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = SecureCookies,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        };

        if (persistent)
            authOptions.Expires = token.ExpiresAtUtc;

        response.Cookies.Append(AuthConstants.AuthCookieName, token.Token, authOptions);

        // Double-submit CSRF token (readable by the SPA so it can echo it in a header).
        response.Cookies.Append(
            AuthConstants.CsrfCookieName,
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            new CookieOptions
            {
                HttpOnly = false,
                Secure = SecureCookies,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                Expires = persistent ? token.ExpiresAtUtc : null
            });
    }

    public void Clear(HttpResponse response)
    {
        response.Cookies.Delete(AuthConstants.AuthCookieName, new CookieOptions { Path = "/", Secure = SecureCookies, SameSite = SameSiteMode.Lax });
        response.Cookies.Delete(AuthConstants.CsrfCookieName, new CookieOptions { Path = "/", Secure = SecureCookies, SameSite = SameSiteMode.Lax });
    }
}
