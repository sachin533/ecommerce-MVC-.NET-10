namespace QuickShop.Api.Security;

/// <summary>Names and policy keys used across the auth stack.</summary>
public static class AuthConstants
{
    /// <summary>HttpOnly cookie carrying the JWT.</summary>
    public const string AuthCookieName = "quickshop_auth";

    /// <summary>Readable cookie carrying the CSRF token (double-submit).</summary>
    public const string CsrfCookieName = "quickshop_xsrf";

    public const string CsrfHeaderName = "X-XSRF-TOKEN";

    /// <summary>HttpContext.Items flag: the JWT arrived via cookie (CSRF applies).</summary>
    public const string AuthViaCookieKey = "QuickShop.AuthViaCookie";

    public const string BearerPrefix = "Bearer ";
}

public static class Policies
{
    /// <summary>Any signed-in shopper (Customer or Admin role).</summary>
    public const string Customer = "Customer";

    /// <summary>Administrators only.</summary>
    public const string Admin = "Admin";
}
