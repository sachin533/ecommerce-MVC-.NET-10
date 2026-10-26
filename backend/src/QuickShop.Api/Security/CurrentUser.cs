using System.Security.Claims;

namespace QuickShop.Api.Security;

/// <summary>Abstraction over the caller's identity taken from validated claims.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    bool IsAdmin { get; }
    string? Email { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    public bool IsAdmin => Principal?.IsInRole("Admin") == true;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);
}
