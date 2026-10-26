using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Api.Services;

namespace QuickShop.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // Paged, filtered, sorted catalog listing.
        group.MapGet("/", ([AsParameters] ProductQuery query, CatalogService catalog, CancellationToken ct) =>
                catalog.SearchAsync(query, ct))
            .AllowAnonymous()
            .Produces<PagedResult<ProductSummaryDto>>();

        group.MapGet("/featured", (CatalogService catalog, CancellationToken ct) =>
                catalog.FeaturedAsync(8, ct))
            .AllowAnonymous()
            .Produces<IReadOnlyList<ProductSummaryDto>>();

        // Filter facets for the sidebar (brands, price range, category counts).
        group.MapGet("/filters", (string? categorySlug, CatalogService catalog, CancellationToken ct) =>
                catalog.FiltersAsync(categorySlug, ct))
            .AllowAnonymous()
            .Produces<ProductFiltersDto>();

        group.MapGet("/{slug}", (string slug, CatalogService catalog, CancellationToken ct) =>
                catalog.GetDetailBySlugAsync(slug, ct: ct))
            .AllowAnonymous()
            .Produces<ProductDetailDto>().ProducesProblem(404);

        group.MapGet("/{id:int}", (int id, CatalogService catalog, CancellationToken ct) =>
                catalog.GetDetailAsync(id, ct: ct))
            .AllowAnonymous()
            .Produces<ProductDetailDto>().ProducesProblem(404);

        group.MapGet("/{id:int}/related", (int id, CatalogService catalog, CancellationToken ct) =>
                catalog.RelatedAsync(id, 4, ct))
            .AllowAnonymous()
            .Produces<IReadOnlyList<ProductSummaryDto>>();

        return app;
    }
}

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", (CategoryService categories, CancellationToken ct) =>
                categories.ListAsync(includeInactive: false, ct))
            .AllowAnonymous()
            .Produces<IReadOnlyList<CategoryDto>>();

        group.MapGet("/{slug}", (string slug, CategoryService categories, CancellationToken ct) =>
                categories.GetBySlugAsync(slug, ct))
            .AllowAnonymous()
            .Produces<CategoryDto>().ProducesProblem(404);

        return app;
    }
}
