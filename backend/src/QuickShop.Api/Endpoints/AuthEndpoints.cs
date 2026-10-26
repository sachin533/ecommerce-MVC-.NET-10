using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services;

namespace QuickShop.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            var response = await auth.RegisterAsync(request, http.Response, ct);
            return Results.Created("/api/auth/me", response);
        }).AllowAnonymous().Produces<AuthResponse>(201).ProducesValidationProblem();

        group.MapPost("/login", async (LoginRequest request, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            var response = await auth.LoginAsync(request, http.Response, ct);
            return Results.Ok(response);
        }).AllowAnonymous().Produces<AuthResponse>().ProducesProblem(401).ProducesValidationProblem();

        group.MapPost("/logout", (AuthService auth, HttpContext http) =>
        {
            auth.Logout(http.Response);
            return Results.Ok(new { message = "Signed out." });
        }).AllowAnonymous().Produces(StatusCodes.Status200OK);

        group.MapPost("/refresh", async (RefreshRequest? request, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var response = await auth.RefreshAsync(request?.RememberMe ?? true, http.Response, ct);
            return Results.Ok(response);
        }).RequireAuthorization().Produces<AuthResponse>().ProducesProblem(401);

        group.MapGet("/me", async (AuthService auth, CancellationToken ct) =>
        {
            return Results.Ok(await auth.GetMeAsync(ct));
        }).RequireAuthorization().Produces<AuthUserDto>().ProducesProblem(401);

        group.MapPut("/profile", async (UpdateProfileRequest request, AuthService auth, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await auth.UpdateProfileAsync(request, ct));
        }).RequireAuthorization(Policies.Customer).Produces<AuthUserDto>().ProducesProblem(401);

        group.MapPost("/change-password", async (ChangePasswordRequest request, AuthService auth, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await auth.ChangePasswordAsync(request, ct));
        }).RequireAuthorization(Policies.Customer).Produces<AuthUserDto>().ProducesProblem(401);

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, AuthService auth, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await auth.ForgotPasswordAsync(request, ct));
        }).AllowAnonymous().Produces<ForgotPasswordResponse>().ProducesValidationProblem();

        group.MapPost("/reset-password", async (ResetPasswordRequest request, AuthService auth, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            await auth.ResetPasswordAsync(request, ct);
            return Results.Ok(new { message = "Password updated. You can now sign in." });
        }).AllowAnonymous().ProducesProblem(200).ProducesValidationProblem();

        return app;
    }
}

/// <summary>Body for silent session renewal.</summary>
public record RefreshRequest(bool RememberMe = true);
