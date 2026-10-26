using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services;

namespace QuickShop.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization(Policies.Customer);

        // Multi-step checkout submits here. Stock is reserved and the mock payment
        // provider is consulted; success/failure reflect the provider's answer.
        group.MapPost("/checkout", async (CheckoutRequest request, OrderService orders, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            var response = await orders.CheckoutAsync(request, ct);
            return Results.Created($"/api/orders/{response.Order.Id}", response);
        }).Produces<CheckoutResponse>(201).ProducesProblem(402).ProducesProblem(409).ProducesValidationProblem();

        group.MapGet("/", (int? page, int? pageSize, OrderService orders, CancellationToken ct) =>
                orders.ListMineAsync(page ?? 1, pageSize ?? 10, ct))
            .Produces<PagedResult<OrderSummaryDto>>();

        group.MapGet("/{id:int}", (int id, OrderService orders, CancellationToken ct) =>
                orders.GetMineAsync(id, ct))
            .Produces<OrderDetailDto>().ProducesProblem(404);

        return app;
    }
}
