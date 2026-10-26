using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services;

namespace QuickShop.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart").WithTags("Cart");

        group.MapGet("/", (HttpContext http, CartService cart, CancellationToken ct) =>
                cart.GetCartAsync(GuestId(http)))
            .AllowAnonymous()
            .Produces<CartDto>();

        group.MapPost("/items", async (AddToCartRequest request, HttpContext http, CartService cart, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await cart.AddItemAsync(GuestId(http), request, ct));
        }).AllowAnonymous().Produces<CartDto>().ProducesProblem(409).ProducesValidationProblem();

        group.MapPut("/items/{id:int}", async (int id, UpdateCartItemRequest request, HttpContext http, CartService cart, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await cart.UpdateItemAsync(GuestId(http), id, request.Quantity, ct));
        }).AllowAnonymous().Produces<CartDto>().ProducesProblem(404).ProducesProblem(409).ProducesValidationProblem();

        group.MapDelete("/items/{id:int}", (int id, HttpContext http, CartService cart, CancellationToken ct) =>
                cart.RemoveItemAsync(GuestId(http), id, ct))
            .AllowAnonymous()
            .Produces<CartDto>().ProducesProblem(404);

        group.MapDelete("/", (HttpContext http, CartService cart, CancellationToken ct) =>
                cart.ClearAsync(GuestId(http), ct))
            .AllowAnonymous()
            .Produces<CartDto>();

        // After sign-in: move the guest cart (matched via X-Cart-Id) into the user's cart.
        group.MapPost("/merge", (HttpContext http, CartService cart, CancellationToken ct) =>
                cart.MergeGuestCartAsync(GuestId(http), ct))
            .RequireAuthorization(Policies.Customer)
            .Produces<CartDto>().ProducesProblem(401);

        return app;
    }

    private static string? GuestId(HttpContext http)
        => http.Request.Headers[CartService.GuestHeader].FirstOrDefault();
}
