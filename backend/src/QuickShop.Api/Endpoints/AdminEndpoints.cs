using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Api.Security;
using QuickShop.Api.Services;

namespace QuickShop.Api.Endpoints;

/// <summary>Protected administration API (/api/admin/*, Admin role only).</summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(Policies.Admin);

        MapDashboard(group);
        MapProducts(group);
        MapCategories(group);
        MapOrders(group);
        MapCustomers(group);

        return app;
    }

    private static void MapDashboard(RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", (DashboardService dashboard, CancellationToken ct) =>
                dashboard.GetStatsAsync(ct))
            .Produces<DashboardStatsDto>();
    }

    private static void MapProducts(RouteGroupBuilder group)
    {
        // Admin catalog listing always includes inactive products.
        group.MapGet("/products", ([AsParameters] ProductQuery query, CatalogService catalog, CancellationToken ct) =>
            {
                query.IncludeInactive = true;
                return catalog.SearchAsync(query, ct);
            })
            .Produces<PagedResult<ProductSummaryDto>>();

        group.MapGet("/products/{id:int}", (int id, CatalogService catalog, CancellationToken ct) =>
                catalog.GetDetailAsync(id, includeInactive: true, ct))
            .Produces<ProductDetailDto>().ProducesProblem(404);

        group.MapPost("/products", async (AdminProductUpsertRequest request, CatalogService catalog, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            var created = await catalog.CreateProductAsync(request, ct);
            return Results.Created($"/api/admin/products/{created.Id}", created);
        }).Produces<ProductDetailDto>(201).ProducesProblem(409).ProducesValidationProblem();

        group.MapPut("/products/{id:int}", async (int id, AdminProductUpsertRequest request, CatalogService catalog, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await catalog.UpdateProductAsync(id, request, ct));
        }).Produces<ProductDetailDto>().ProducesProblem(404).ProducesProblem(409).ProducesValidationProblem();

        group.MapDelete("/products/{id:int}", async (int id, CatalogService catalog, CancellationToken ct) =>
        {
            await catalog.DeleteProductAsync(id, ct);
            return Results.NoContent();
        }).ProducesProblem(204).ProducesProblem(404);

        group.MapPatch("/products/{id:int}/stock", async (int id, InventoryUpdateRequest request, CatalogService catalog, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await catalog.SetStockAsync(id, request.Stock, ct));
        }).Produces<ProductDetailDto>().ProducesProblem(404).ProducesValidationProblem();
    }

    private static void MapCategories(RouteGroupBuilder group)
    {
        group.MapGet("/categories", (CategoryService categories, CancellationToken ct) =>
                categories.ListAsync(includeInactive: true, ct))
            .Produces<IReadOnlyList<CategoryDto>>();

        group.MapPost("/categories", async (CategoryUpsertRequest request, CategoryService categories, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            var created = await categories.CreateAsync(request, ct);
            return Results.Created($"/api/admin/categories/{created.Id}", created);
        }).Produces<CategoryDto>(201).ProducesProblem(409).ProducesValidationProblem();

        group.MapPut("/categories/{id:int}", async (int id, CategoryUpsertRequest request, CategoryService categories, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await categories.UpdateAsync(id, request, ct));
        }).Produces<CategoryDto>().ProducesProblem(404).ProducesProblem(409).ProducesValidationProblem();

        group.MapDelete("/categories/{id:int}", async (int id, CategoryService categories, CancellationToken ct) =>
        {
            await categories.DeleteAsync(id, ct);
            return Results.NoContent();
        }).ProducesProblem(204).ProducesProblem(404).ProducesProblem(409);
    }

    private static void MapOrders(RouteGroupBuilder group)
    {
        group.MapGet("/orders", (string? status, string? search, int? page, int? pageSize, OrderService orders, CancellationToken ct) =>
                orders.ListAdminAsync(status, search, page ?? 1, pageSize ?? 15, ct))
            .Produces<PagedResult<OrderSummaryDto>>();

        group.MapGet("/orders/{id:int}", (int id, OrderService orders, CancellationToken ct) =>
                orders.GetAdminAsync(id, ct))
            .Produces<OrderDetailDto>().ProducesProblem(404);

        group.MapPut("/orders/{id:int}/status", async (int id, OrderStatusUpdateRequest request, OrderService orders, CancellationToken ct) =>
        {
            if (!request.Validate(out var errors))
                return Results.ValidationProblem(errors);

            return Results.Ok(await orders.UpdateStatusAsync(id, request.Status, ct));
        }).Produces<OrderDetailDto>().ProducesProblem(404).ProducesProblem(409).ProducesValidationProblem();
    }

    private static void MapCustomers(RouteGroupBuilder group)
    {
        group.MapGet("/customers", (string? search, int? page, int? pageSize, CustomerService customers, CancellationToken ct) =>
                customers.ListAsync(search, page ?? 1, pageSize ?? 20, ct))
            .Produces<PagedResult<CustomerDto>>();
    }
}
