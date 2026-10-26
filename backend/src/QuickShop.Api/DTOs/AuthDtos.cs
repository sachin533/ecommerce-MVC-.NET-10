using System.ComponentModel.DataAnnotations;
using QuickShop.Api.Common;

namespace QuickShop.Api.DTOs;

public record RegisterRequest(
    [property: Required(ErrorMessage = "Full name is required.")]
    [property: StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
    string FullName,

    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [property: StringLength(256, ErrorMessage = "Email is too long.")]
    string Email,

    [property: Required(ErrorMessage = "Password is required.")]
    [property: MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [property: MaxLength(128, ErrorMessage = "Password is too long.")]
    string Password,

    [property: Required(ErrorMessage = "Confirm your password.")]
    [property: Compare("Password", ErrorMessage = "Passwords do not match.")]
    string ConfirmPassword,

    [property: MustBeTrue(ErrorMessage = "You must accept the terms to create an account.")]
    bool AcceptTerms);

public record LoginRequest(
    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "Enter a valid email address.")]
    string Email,

    [property: Required(ErrorMessage = "Password is required.")]
    string Password,

    bool RememberMe = false);

public record ForgotPasswordRequest(
    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "Enter a valid email address.")]
    string Email);

public record ResetPasswordRequest(
    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "Enter a valid email address.")]
    string Email,

    [property: Required(ErrorMessage = "Reset token is required.")]
    string Token,

    [property: Required(ErrorMessage = "Password is required.")]
    [property: MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    string NewPassword,

    [property: Required(ErrorMessage = "Confirm your password.")]
    [property: Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    string ConfirmNewPassword);

public record ChangePasswordRequest(
    [property: Required] string CurrentPassword,
    [property: Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")] string NewPassword,
    [property: Required, Compare("NewPassword", ErrorMessage = "Passwords do not match.")] string ConfirmNewPassword);

public record UpdateProfileRequest(
    [property: Required, StringLength(200, MinimumLength = 2)] string FullName,
    [property: StringLength(30)] string? PhoneNumber);

public record AuthUserDto(
    string Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles);

public record AuthResponse(AuthUserDto User, DateTime ExpiresAtUtc);

public record ForgotPasswordResponse(string Message, string? DevResetToken = null);
