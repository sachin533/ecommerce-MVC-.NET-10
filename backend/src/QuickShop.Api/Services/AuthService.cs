using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuickShop.Api.Common;
using QuickShop.Api.Data;
using QuickShop.Api.Domain;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services.Email;

namespace QuickShop.Api.Services;

/// <summary>
/// Identity-backed authentication. Tokens are issued only after the credentials are
/// verified against Identity's password hasher and delivered via HttpOnly cookie.
/// </summary>
public class AuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string CustomerRole = "Customer";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IAuthCookieService _cookieService;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentUser _currentUser;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IAuthCookieService cookieService,
        IEmailSender emailSender,
        ICurrentUser currentUser,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _cookieService = cookieService;
        _emailSender = emailSender;
        _currentUser = currentUser;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, HttpResponse response, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new ConflictException("An account with this email already exists. Try signing in instead.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true, // email delivery is dev-only; Identity still enforces password rules
            FullName = request.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationException("Please correct the errors below.",
                result.Errors.GroupBy(e => MapErrorMember(e.Code))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        if (!await _roleManager.RoleExistsAsync(CustomerRole))
            await _roleManager.CreateAsync(new IdentityRole(CustomerRole));
        await _userManager.AddToRoleAsync(user, CustomerRole);

        return await IssueAuthResponseAsync(user, response, persistent: false);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, HttpResponse response, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Identical message regardless of whether the account exists.
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException("Too many failed sign-in attempts. Please wait a few minutes and try again.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await IssueAuthResponseAsync(user, response, request.RememberMe);
    }

    /// <summary>Issues a fresh token for an already authenticated session (sliding expiry).</summary>
    public async Task<AuthResponse> RefreshAsync(bool persistent, HttpResponse response, CancellationToken ct = default)
    {
        var user = await LoadCurrentUserAsync()
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        return await IssueAuthResponseAsync(user, response, persistent);
    }

    public void Logout(HttpResponse response) => _cookieService.Clear(response);

    public async Task<AuthUserDto> GetMeAsync(CancellationToken ct = default)
    {
        var user = await LoadCurrentUserAsync()
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);
        return await ToDtoAsync(user);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontend = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var link = $"{frontend.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            await _emailSender.SendAsync(new OutboundEmail(
                user.Email!,
                "Reset your QuickShop password",
                $"Hi {user.FullName},\n\nUse the link below to reset your password. It expires shortly and can only be used once:\n\n{link}\n\nIf you didn't request this, you can ignore this email."), ct);

            _logger.LogInformation("Password reset generated for {Email}", user.Email);

            // DEVELOPMENT-ONLY escape hatch so the reset flow is testable without SMTP.
            if (_environment.IsDevelopment())
                return new ForgotPasswordResponse(
                    "If an account exists for that email, a reset link has been generated. " +
                    "Development: no real email is sent — see the response field devResetToken, logs/dev-emails/ or the console.",
                    token);
        }

        // Same message whether or not the account exists (prevents enumeration).
        return new ForgotPasswordResponse(
            "If an account exists for that email, a reset link has been generated. " +
            "Development: no real email is sent — see the response field devResetToken, logs/dev-emails/ or the console.");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new ValidationException("Invalid or expired reset link.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException("Invalid or expired reset link.",
                result.Errors.GroupBy(e => MapErrorMember(e.Code))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        _logger.LogInformation("Password reset completed for {Email}", user.Email);
    }

    public async Task<AuthUserDto> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await LoadCurrentUserAsync()
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException("Could not change the password.",
                result.Errors.GroupBy(e => MapErrorMember(e.Code, currentPassword: true))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        return await ToDtoAsync(user);
    }

    public async Task<AuthUserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await LoadCurrentUserAsync()
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new ValidationException("Could not update the profile.",
                result.Errors.GroupBy(e => MapErrorMember(e.Code))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        return await ToDtoAsync(user);
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<ApplicationUser?> LoadCurrentUserAsync()
    {
        var id = _currentUser.UserId;
        return id is null ? null : await _userManager.FindByIdAsync(id);
    }

    private async Task<AuthResponse> IssueAuthResponseAsync(ApplicationUser user, HttpResponse response, bool persistent)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var issued = _tokenService.CreateToken(user, roles.ToList());
        _cookieService.Attach(response, issued, persistent);
        return new AuthResponse(await ToDtoAsync(user), issued.ExpiresAtUtc);
    }

    private async Task<AuthUserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.PhoneNumber,
            roles.ToList());
    }

    private static string MapErrorMember(string code, bool currentPassword = false) => code switch
    {
        "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit"
            or "PasswordRequiresUpper" or "PasswordRequiresLower" or "PasswordRequiresUniqueChars" => "Password",
        "PasswordMismatch" => currentPassword ? "CurrentPassword" : "ConfirmPassword",
        "InvalidToken" => "Token",
        "DuplicateUserName" or "InvalidEmail" or "UserAlreadyHasPassword" => "Email",
        _ => "request"
    };
}
