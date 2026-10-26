using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Tests.Infrastructure;
using Xunit;

namespace QuickShop.Tests.Integration;

[Collection(IntegrationFixture.Name)]
public class OrderAdminTests
{
    private readonly QuickShopApiFactory _factory;

    public OrderAdminTests(QuickShopApiFactory factory) => _factory = factory;

    /// <summary>Places one paid order for a brand-new customer and returns the API client + order.</summary>
    private async Task<(ApiClient Api, OrderDetailDto Order)> PlacePaidOrderAsync(string cardLast4 = "4242")
    {
        var api = new ApiClient(_factory);
        await api.RegisterAsync($"flow.{Guid.NewGuid():N}@test.dev", "Flow@12345");

        var anon = _factory.CreateClient();
        var productPage = await anon.GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            "/api/products?sort=price-asc&pageSize=1&inStockOnly=true");
        var product = productPage!.Items[0];

        await api.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 1 });
        var checkout = await api.PostAsync("/api/orders/checkout", new
        {
            addressId = (int?)null,
            newAddress = new
            {
                fullName = "Flow Buyer",
                phone = "9123456780",
                line1 = "9 Admin Road",
                city = "Pune",
                state = "Maharashtra",
                postalCode = "411001",
                country = "India"
            },
            shippingMethod = "standard",
            paymentMethod = "mock_card",
            cardLast4,
            customerNote = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);

        var response = await checkout.Content.ReadFromJsonAsync<CheckoutResponse>();
        return (api, response!.Order);
    }

    private async Task<ApiClient> AdminAsync()
    {
        var admin = new ApiClient(_factory);
        await admin.LoginAsync("admin@quickshop.dev", "Admin@12345");
        return admin;
    }

    [Fact]
    public async Task Admin_can_walk_an_order_from_paid_to_delivered()
    {
        var (buyer, order) = await PlacePaidOrderAsync();
        using var _ = buyer;
        using var admin = await AdminAsync();

        foreach (var status in new[] { "processing", "shipped", "delivered" })
        {
            var response = await admin.PutAsync($"/api/admin/orders/{order.Id}/status", new { status });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var detail = await admin.ReadAsync<OrderDetailDto>(await admin.GetAsync($"/api/admin/orders/{order.Id}"));
        Assert.Equal("Delivered", detail!.Status);
    }

    [Fact]
    public async Task Invalid_status_transition_is_409()
    {
        var (buyer, order) = await PlacePaidOrderAsync();
        using var _ = buyer;
        using var admin = await AdminAsync();

        // Paid orders cannot jump straight to Delivered (must ship first).
        var response = await admin.PutAsync($"/api/admin/orders/{order.Id}/status", new { status = "delivered" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_status_string_is_a_validation_error()
    {
        var (buyer, order) = await PlacePaidOrderAsync();
        using var _ = buyer;
        using var admin = await AdminAsync();

        var response = await admin.PutAsync($"/api/admin/orders/{order.Id}/status", new { status = "teleported" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancelling_a_paid_order_restores_stock()
    {
        var (buyer, order) = await PlacePaidOrderAsync();
        using var _ = buyer;
        using var admin = await AdminAsync();

        var line = order.Items[0];
        var anon = _factory.CreateClient();

        // Checkout decremented stock by 1; cancelling must give it back.
        var beforeCancel = await anon.GetFromJsonAsync<ProductDetailDto>($"/api/products/{line.Slug}");
        var response = await admin.PutAsync($"/api/admin/orders/{order.Id}/status", new { status = "cancelled" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var afterCancel = await anon.GetFromJsonAsync<ProductDetailDto>($"/api/products/{line.Slug}");
        Assert.Equal(beforeCancel!.Stock + 1, afterCancel!.Stock);

        var detail = await admin.ReadAsync<OrderDetailDto>(await admin.GetAsync($"/api/admin/orders/{order.Id}"));
        Assert.Equal("Cancelled", detail!.Status);
    }

    [Fact]
    public async Task Customer_cannot_read_another_customers_order()
    {
        var (buyer, order) = await PlacePaidOrderAsync();
        using var _ = buyer;

        var other = new ApiClient(_factory);
        await other.RegisterAsync($"nosy.{Guid.NewGuid():N}@test.dev", "Nosy@12345");

        // Looks like a 404, not a 403 — no existence leak.
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/orders/{order.Id}")).StatusCode);
    }

    [Fact]
    public async Task Customer_is_blocked_from_admin_endpoints()
    {
        var (buyer, _) = await PlacePaidOrderAsync();
        using var _ = buyer;

        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.GetAsync("/api/admin/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await buyer.PostAsync("/api/admin/products", new { })).StatusCode);
    }

    [Fact]
    public async Task Anonymous_is_unauthenticated_on_admin_endpoints()
    {
        using var anon = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/admin/dashboard")).StatusCode);
    }

    [Fact]
    public async Task Admin_dashboard_reports_the_seeded_business()
    {
        using var admin = await AdminAsync();
        var response = await admin.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await admin.ReadAsync<DashboardStatsDto>(response);

        Assert.True(stats!.TotalProducts >= 120, $"expected the 186-product seed, got {stats.TotalProducts}");
        Assert.True(stats.TotalCustomers >= 4);
        Assert.True(stats.TotalOrders >= 17);
        Assert.NotEmpty(stats.RecentOrders);
    }

    [Fact]
    public async Task Admin_can_create_update_and_soft_delete_a_product()
    {
        using var admin = await AdminAsync();
        var categories = await admin.ReadAsync<List<CategoryDto>>(await admin.GetAsync("/api/admin/categories"));
        var categoryId = categories![0].Id;

        // Create
        var created = await admin.PostAsync("/api/admin/products", new
        {
            name = $"Zebra Phone {Guid.NewGuid():N}".Substring(0, 30),
            brand = "QuickShop Test",
            description = "A product created by the integration test suite to exercise admin CRUD.",
            price = 4999m,
            discountPercent = 10m,
            stock = 5,
            rating = 4.2m,
            ratingCount = 10,
            categoryId,
            imageUrl = "https://placehold.co/600x600.png",
            sku = (string?)null,
            isFeatured = false,
            isActive = true,
            images = (List<string>?)null,
            variants = (List<VariantUpsertRequest>?)null
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var product = await created.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal(4499m, product!.SalePrice); // 4999 - 10% = 4499.1 -> rounds to 4499

        // Update stock (inventory management)
        var updated = await admin.PutAsync($"/api/admin/products/{product.Id}", new
        {
            name = product.Name,
            brand = product.Brand,
            description = product.Description,
            price = 4999m,
            discountPercent = 10m,
            stock = 42,
            rating = product.Rating,
            ratingCount = product.RatingCount,
            categoryId,
            imageUrl = product.ImageUrl,
            sku = product.Sku,
            isFeatured = false,
            isActive = true,
            images = (List<string>?)null,
            variants = (List<VariantUpsertRequest>?)null
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var afterStock = await admin.ReadAsync<ProductDetailDto>(await admin.GetAsync($"/api/admin/products/{product.Id}"));
        Assert.Equal(42, afterStock!.Stock);

        // Soft delete: hidden from the public storefront, retained for order history.
        var deleted = await admin.DeleteAsync($"/api/admin/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var storefront = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await storefront.GetAsync($"/api/products/{product.Slug}")).StatusCode);

        var kept = await admin.ReadAsync<ProductDetailDto>(await admin.GetAsync($"/api/admin/products/{product.Id}"));
        Assert.False(kept!.IsActive);
    }

    [Fact]
    public async Task Admin_can_manage_categories()
    {
        using var admin = await AdminAsync();

        var created = await admin.PostAsync("/api/admin/categories", new
        {
            name = $"Test Category {Guid.NewGuid():N}".Substring(0, 26),
            description = "Created and deleted by integration tests.",
            imageUrl = (string?)null,
            isActive = true,
            sortOrder = 99
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var category = await created.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.False(string.IsNullOrWhiteSpace(category!.Slug));

        var renamed = await admin.PutAsync($"/api/admin/categories/{category.Id}", new
        {
            name = category.Name + " (renamed)",
            description = category.Description,
            imageUrl = category.ImageUrl,
            isActive = true,
            sortOrder = 99
        });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        var deleted = await admin.DeleteAsync($"/api/admin/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Admin_customer_list_includes_seeded_shoppers()
    {
        using var admin = await AdminAsync();
        var page = await admin.ReadAsync<PagedResult<CustomerDto>>(await admin.GetAsync("/api/admin/customers?pageSize=50"));

        Assert.True(page!.TotalCount >= 4);
        Assert.Contains(page.Items, c => c.Email == "rahul.sharma@demo.dev");
        Assert.Contains(page.Items, c => c.Email == "admin@quickshop.dev");
    }
}
