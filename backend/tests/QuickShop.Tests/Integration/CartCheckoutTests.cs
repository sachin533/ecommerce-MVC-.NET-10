using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using QuickShop.Api.Common;
using QuickShop.Api.DTOs;
using QuickShop.Tests.Infrastructure;
using Xunit;

namespace QuickShop.Tests.Integration;

[Collection(IntegrationFixture.Name)]
public class CartCheckoutTests
{
    private readonly QuickShopApiFactory _factory;

    public CartCheckoutTests(QuickShopApiFactory factory) => _factory = factory;

    private async Task<ProductSummaryDto> CheapestInStockAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            "/api/products?sort=price-asc&pageSize=1&inStockOnly=true");
        return page!.Items[0];
    }

    private static object Address() => new
    {
        fullName = "Test Buyer",
        phone = "9876543210",
        line1 = "12 Test Street",
        city = "Bengaluru",
        state = "Karnataka",
        postalCode = "560001",
        country = "India"
    };

    [Fact]
    public async Task Guest_can_add_update_and_remove_items()
    {
        using var api = new ApiClient(_factory);
        using var anon = _factory.CreateClient();
        var product = await CheapestInStockAsync(anon);

        var added = await api.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);

        var cart = await api.ReadAsync<CartDto>(await api.GetAsync("/api/cart"));
        Assert.Single(cart!.Items);
        Assert.Equal(2, cart.Items[0].Quantity);
        // Server computes prices; the client never sends them.
        Assert.Equal(product.SalePrice, cart.Items[0].UnitPrice);
        Assert.Equal(product.SalePrice * 2, cart.Subtotal);

        var lineId = cart.Items[0].Id;
        var updated = await api.PutAsync($"/api/cart/items/{lineId}", new { quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        cart = await api.ReadAsync<CartDto>(await api.GetAsync("/api/cart"));
        Assert.Equal(1, cart!.Items[0].Quantity);
        Assert.Equal(cart.Items[0].UnitPrice, cart.Items[0].LineTotal);

        var removed = await api.DeleteAsync($"/api/cart/items/{lineId}");
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);

        cart = await api.ReadAsync<CartDto>(await api.GetAsync("/api/cart"));
        Assert.True(cart!.IsEmpty);
    }

    [Fact]
    public async Task One_guest_cart_cannot_touch_another_guests_cart()
    {
        using var owner = new ApiClient(_factory);
        using var intruder = new ApiClient(_factory);
        using var anon = _factory.CreateClient();
        var product = await CheapestInStockAsync(anon);

        await owner.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 1 });
        var ownerCart = await owner.ReadAsync<CartDto>(await owner.GetAsync("/api/cart"));
        var lineId = ownerCart!.Items[0].Id;

        // The intruder sends a different X-Cart-Id, so the line belongs to nobody's cart.
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PutAsync($"/api/cart/items/{lineId}", new { quantity = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/cart/items/{lineId}")).StatusCode);

        // The owner's line is untouched.
        var still = await owner.ReadAsync<CartDto>(await owner.GetAsync("/api/cart"));
        Assert.Equal(1, still!.Items[0].Quantity);
    }

    /// <summary>
    /// Regression: merging a guest cart that was never written (a fresh visitor
    /// who signs in without shopping first) used to throw because the merge
    /// transaction ran outside CreateExecutionStrategy under EnableRetryOnFailure.
    /// </summary>
    [Fact]
    public async Task Merge_with_an_empty_guest_cart_succeeds()
    {
        using var api = new ApiClient(_factory);
        await api.RegisterAsync($"merger.{Guid.NewGuid():N}@test.dev", "Buyer@123");

        var merged = await api.PostAsync("/api/cart/merge", new { });

        Assert.Equal(HttpStatusCode.OK, merged.StatusCode);
        var cart = await api.ReadAsync<CartDto>(merged);
        Assert.True(cart!.IsEmpty);
        Assert.Equal("user", cart.Owner);
    }

    [Fact]
    public async Task Merge_folds_guest_items_into_the_user_cart()
    {
        using var guest = new ApiClient(_factory);
        using var anon = _factory.CreateClient();
        var product = await CheapestInStockAsync(anon);
        await guest.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 2 });

        // Sign in on the same client, then merge the guest cart.
        await guest.RegisterAsync($"fold.{Guid.NewGuid():N}@test.dev", "Buyer@123");
        var merged = await guest.PostAsync("/api/cart/merge", new { });

        Assert.Equal(HttpStatusCode.OK, merged.StatusCode);
        var cart = await guest.ReadAsync<CartDto>(merged);
        Assert.Single(cart!.Items);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal(product.Id, cart.Items[0].ProductId);
    }

    [Fact]
    public async Task Quantity_above_stock_is_rejected_with_409()
    {
        using var api = new ApiClient(_factory);
        using var anon = _factory.CreateClient();
        var product = await CheapestInStockAsync(anon);

        var response = await api.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 20 });

        // Either the product has < 20 units (409) or plenty of stock (200); never a server error.
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
            $"Unexpected status {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Checkout_success_marks_order_paid_and_clears_cart()
    {
        using var api = new ApiClient(_factory);
        await api.RegisterAsync($"buyer.{Guid.NewGuid():N}@test.dev", "Buyer@123");
        using var anon = _factory.CreateClient();
        var product = await CheapestInStockAsync(anon);

        var stockBefore = product.Stock;
        await api.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 2 });

        var checkout = await api.PostAsync("/api/orders/checkout", new
        {
            addressId = (int?)null,
            newAddress = Address(),
            shippingMethod = "standard",
            paymentMethod = "mock_card",
            cardLast4 = "4242", // even -> mock provider approves
            customerNote = "integration test"
        });

        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);

        var body = await checkout.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.Equal("Paid", body!.Order.Status);
        Assert.Equal("Succeeded", body.Payment.Status);
        Assert.NotNull(body.Payment.TransactionId);
        Assert.Equal("4242", body.Payment.CardLast4);
        Assert.Matches(@"^ORD\d{7}$", body.Order.OrderNumber);

        // Totals are server-derived: subtotal + standard shipping (₹49 under ₹999, free at/above).
        var expectedShipping = body.Order.Subtotal >= 999m ? 0m : 49m;
        Assert.Equal(product.SalePrice * 2, body.Order.Subtotal);
        Assert.Equal(body.Order.Subtotal + expectedShipping, body.Order.Total);

        // Cart cleared, stock decremented.
        var cart = await api.ReadAsync<CartDto>(await api.GetAsync("/api/cart"));
        Assert.True(cart!.IsEmpty);

        var after = await anon.GetFromJsonAsync<ProductDetailDto>($"/api/products/{product.Slug}");
        Assert.Equal(stockBefore - 2, after!.Stock);

        // Order shows up in the customer's history.
        var history = await api.ReadAsync<PagedResult<OrderSummaryDto>>(await api.GetAsync("/api/orders"));
        Assert.Contains(history!.Items, o => o.OrderNumber == body.Order.OrderNumber);
    }

    [Fact]
    public async Task Declined_payment_returns_402_and_restores_everything()
    {
        using var api = new ApiClient(_factory);
        await api.RegisterAsync($"decliner.{Guid.NewGuid():N}@test.dev", "Buyer@123");
        using var anon = _factory.CreateClient();
        var product = await CheapestInStockAsync(anon);

        var stockBefore = product.Stock;
        await api.PostAsync("/api/cart/items", new { productId = product.Id, quantity = 1 });

        var checkout = await api.PostAsync("/api/orders/checkout", new
        {
            addressId = (int?)null,
            newAddress = Address(),
            shippingMethod = "express",
            paymentMethod = "mock_card",
            cardLast4 = "4111", // odd -> mock provider declines
            customerNote = (string?)null
        });

        Assert.Equal(HttpStatusCode.PaymentRequired, checkout.StatusCode);

        var problem = await ApiClient.ReadJsonAsync(checkout);
        Assert.Equal(402, problem.GetProperty("status").GetInt32());
        Assert.Matches(@"^ORD\d{7}$", problem.GetProperty("orderNumber").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("providerReason").GetString()));

        // Stock restored to exactly what it was.
        var after = await anon.GetFromJsonAsync<ProductDetailDto>($"/api/products/{product.Slug}");
        Assert.Equal(stockBefore, after!.Stock);

        // Cart preserved so the shopper can retry.
        var cart = await api.ReadAsync<CartDto>(await api.GetAsync("/api/cart"));
        Assert.Single(cart!.Items);

        // The order exists but is Cancelled, and its payment is Failed.
        var history = await api.ReadAsync<PagedResult<OrderSummaryDto>>(await api.GetAsync("/api/orders"));
        var cancelled = history!.Items.Single(o => o.OrderNumber == problem.GetProperty("orderNumber").GetString());
        Assert.Equal("Cancelled", cancelled.Status);

        var detail = await api.ReadAsync<OrderDetailDto>(
            await api.GetAsync($"/api/orders/{cancelled.Id}"));
        Assert.Equal("Failed", detail!.Payment!.Status);
        Assert.NotNull(detail.Payment.FailureReason);
    }

    [Fact]
    public async Task Checkout_with_empty_cart_is_a_validation_error()
    {
        using var api = new ApiClient(_factory);
        await api.RegisterAsync($"empty.{Guid.NewGuid():N}@test.dev", "Buyer@123");

        var response = await api.PostAsync("/api/orders/checkout", new
        {
            addressId = (int?)null,
            newAddress = Address(),
            shippingMethod = "standard",
            paymentMethod = "mock_card",
            cardLast4 = "4242",
            customerNote = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_requires_authentication()
    {
        using var api = new ApiClient(_factory); // never logs in

        var response = await api.PostAsync("/api/orders/checkout", new
        {
            addressId = (int?)null,
            newAddress = Address(),
            shippingMethod = "standard",
            paymentMethod = "mock_card",
            cardLast4 = "4242",
            customerNote = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
