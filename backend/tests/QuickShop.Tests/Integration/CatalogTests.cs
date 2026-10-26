using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Tests.Infrastructure;
using Xunit;

namespace QuickShop.Tests.Integration;

[Collection(IntegrationFixture.Name)]
public class CatalogTests
{
    private readonly QuickShopApiFactory _factory;

    public CatalogTests(QuickShopApiFactory factory) => _factory = factory;

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions());

    [Fact]
    public async Task Seed_loads_the_full_catalog()
    {
        using var client = CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?page=1&pageSize=1");

        Assert.Equal(186, page!.TotalCount);
        Assert.True(page.TotalPages >= 16); // 186 / 12-per-page
    }

    [Fact]
    public async Task Seed_loads_every_category()
    {
        using var client = CreateClient();
        var categories = await client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories");

        Assert.Equal(14, categories!.Count);
        Assert.All(categories, c => Assert.False(string.IsNullOrWhiteSpace(c.Slug)));
    }

    [Fact]
    public async Task Search_returns_only_matches()
    {
        using var client = CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?search=iphone&pageSize=60");

        Assert.True(page!.TotalCount > 0);
        Assert.All(page.Items, p =>
            Assert.Contains("iphone", p.Name + p.Brand, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Category_filter_returns_products_from_that_category()
    {
        using var client = CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?categorySlug=laptops&pageSize=60");

        Assert.True(page!.TotalCount > 0);
        Assert.All(page.Items, p => Assert.Equal("laptops", p.CategorySlug));
    }

    [Fact]
    public async Task Price_sorting_is_applied_server_side()
    {
        using var client = CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?sort=price-asc&pageSize=20");

        var prices = page!.Items.Select(p => p.SalePrice).ToList();
        Assert.Equal(prices.OrderBy(p => p), prices);
    }

    [Fact]
    public async Task Invalid_page_inputs_are_clamped_not_rejected()
    {
        using var client = CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?page=-5&pageSize=9999");

        Assert.Equal(1, page!.Page);
        Assert.True(page.PageSize <= 60); // server-side cap
    }

    [Fact]
    public async Task Filters_endpoint_returns_brands_categories_and_price_range()
    {
        using var client = CreateClient();
        var filters = await client.GetFromJsonAsync<ProductFiltersDto>("/api/products/filters");

        Assert.NotEmpty(filters!.Brands);
        Assert.NotEmpty(filters.Categories);
        Assert.True(filters.PriceRange.Max >= filters.PriceRange.Min);
    }

    [Fact]
    public async Task Product_detail_by_slug_includes_images_or_falls_back_gracefully()
    {
        using var client = CreateClient();
        var list = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?pageSize=1");
        var slug = list!.Items[0].Slug;

        var detail = await client.GetFromJsonAsync<ProductDetailDto>($"/api/products/{slug}");

        Assert.Equal(slug, detail!.Slug);
        Assert.False(string.IsNullOrWhiteSpace(detail.Description));
        // Images may be empty — the UI must render a fallback, never a broken layout.
        Assert.NotNull(detail.Images);
    }

    [Fact]
    public async Task Unknown_product_slug_is_404()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/api/products/definitely-not-a-real-product");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Featured_products_are_marked_featured()
    {
        using var client = CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?featured=true&pageSize=12");

        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, p => Assert.True(p.IsFeatured));
    }
}
