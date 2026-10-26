using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services;

namespace QuickShop.Api.Endpoints;

public static class AddressEndpoints
{
    public static IEndpointRouteBuilder MapAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/addresses")
            .WithTags("Addresses")
            .RequireAuthorization(Policies.Customer);

        group.MapGet("/", (AddressService addresses, CancellationToken ct) =>
                addresses.ListAsync(ct))
            .Produces<IReadOnlyList<AddressDto>>();

        group.MapPost("/", async (AddressUpsertRequest request, AddressService addresses, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            var created = await addresses.CreateAsync(request, ct);
            return Results.Created($"/api/addresses/{created.Id}", created);
        }).Produces<AddressDto>(201).ProducesValidationProblem();

        group.MapPut("/{id:int}", async (int id, AddressUpsertRequest request, AddressService addresses, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await addresses.UpdateAsync(id, request, ct));
        }).Produces<AddressDto>().ProducesProblem(404).ProducesValidationProblem();

        group.MapPost("/{id:int}/default", (int id, AddressService addresses, CancellationToken ct) =>
                addresses.SetDefaultAsync(id, ct))
            .Produces<AddressDto>().ProducesProblem(404);

        group.MapDelete("/{id:int}", async (int id, AddressService addresses, CancellationToken ct) =>
        {
            await addresses.DeleteAsync(id, ct);
            return Results.NoContent();
        }).ProducesProblem(204).ProducesProblem(404);

        return app;
    }
}

public static class WishlistEndpoints
{
    public static IEndpointRouteBuilder MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wishlist")
            .WithTags("Wishlist")
            .RequireAuthorization(Policies.Customer);

        group.MapGet("/", (WishlistService wishlist, CancellationToken ct) =>
                wishlist.GetAsync(ct))
            .Produces<WishlistDto>();

        group.MapPost("/items/{productId:int}", (int productId, WishlistService wishlist, CancellationToken ct) =>
                wishlist.AddAsync(productId, ct))
            .Produces<WishlistDto>().ProducesProblem(404);

        group.MapDelete("/items/{productId:int}", (int productId, WishlistService wishlist, CancellationToken ct) =>
                wishlist.RemoveAsync(productId, ct))
            .Produces<WishlistDto>();

        return app;
    }
}
