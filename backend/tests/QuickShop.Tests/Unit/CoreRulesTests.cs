using QuickShop.Api.Common;
using QuickShop.Api.Domain;

namespace QuickShop.Tests.Unit;

public class PricingTests
{
    [Theory]
    [InlineData(1000, 0, 1000)]
    [InlineData(1000, 10, 900)]
    [InlineData(999, 25, 749)]      // 749.25 rounds away from zero -> 749? No: 999*75/100 = 749.25 -> 749
    [InlineData(499, 50, 250)]      // 249.5 rounds away from zero -> 250
    [InlineData(231, 0, 231)]
    public void EffectivePrice_subtracts_discount_and_rounds_to_whole_rupees(decimal mrp, decimal discount, decimal expected)
    {
        Assert.Equal(expected, Pricing.EffectivePrice(mrp, discount));
    }

    [Theory]
    [InlineData(1000, 95)]  // clamped to 90% -> 100
    [InlineData(1000, -5)]  // clamped to 0%  -> 1000
    public void EffectivePrice_clamps_discount_percent_to_valid_range(decimal mrp, decimal discount)
    {
        var expected = discount > 90 ? 100m : mrp;
        Assert.Equal(expected, Pricing.EffectivePrice(mrp, discount));
    }

    [Fact]
    public void EffectivePrice_of_zero_price_is_zero()
    {
        Assert.Equal(0m, Pricing.EffectivePrice(0m, 20m));
    }

    [Fact]
    public void ShippingCost_standard_is_free_at_or_above_threshold()
    {
        Assert.Equal(Pricing.StandardShippingCost, Pricing.ShippingCost(ShippingMethod.Standard, Pricing.FreeShippingThreshold - 0.01m));
        Assert.Equal(0m, Pricing.ShippingCost(ShippingMethod.Standard, Pricing.FreeShippingThreshold));
        Assert.Equal(0m, Pricing.ShippingCost(ShippingMethod.Standard, 5000m));
    }

    [Fact]
    public void ShippingCost_express_is_always_the_express_rate()
    {
        Assert.Equal(Pricing.ExpressShippingCost, Pricing.ShippingCost(ShippingMethod.Express, 100m));
        Assert.Equal(Pricing.ExpressShippingCost, Pricing.ShippingCost(ShippingMethod.Express, 50_000m));
    }
}

public class OrderStatusTransitionsTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]   // must be paid first
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Delivered, false)] // must ship first
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Processing, false)] // terminal
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]       // terminal
    public void CanTransition_enforces_the_business_rule(OrderStatus from, OrderStatus to, bool allowed)
    {
        Assert.Equal(allowed, OrderStatusTransitions.CanTransition(from, to));
    }

    [Fact]
    public void SameStatusIsAlwaysAllowed()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
            Assert.True(OrderStatusTransitions.CanTransition(status, status));
    }
}

public class SlugifyTests
{
    [Theory]
    [InlineData("Wireless Headphones", "wireless-headphones")]
    [InlineData("  iPhone 15 Pro  ", "iphone-15-pro")]
    [InlineData("Kids_toys & Games", "kids-toys-games")]
    [InlineData("Multi---Dash", "multi-dash")]
    [InlineData("---edge---", "edge")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Slugify_produces_url_safe_slugs(string input, string expected)
    {
        Assert.Equal(expected, Text.Slugify(input));
    }

    [Fact]
    public void Slugify_never_embeds_nul_characters()
    {
        Assert.DoesNotContain('\0', Text.Slugify("Kids' Toys (2024) #1!"));
    }
}

public class PaginationTests
{
    [Theory]
    [InlineData(0, 0, 1, Pagination.DefaultPageSize)]
    [InlineData(-3, -7, 1, Pagination.DefaultPageSize)]
    [InlineData(2, 25, 2, 25)]
    [InlineData(1, 500, 1, Pagination.MaxPageSize)] // oversized page sizes are capped
    public void Normalize_clamps_page_and_page_size(int page, int pageSize, int expectedPage, int expectedSize)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        Assert.Equal(expectedPage, page);
        Assert.Equal(expectedSize, pageSize);
    }

    [Fact]
    public void PagedResult_computes_total_pages_and_navigation_flags()
    {
        var result = PagedResult<int>.Create([1, 2, 3], page: 2, pageSize: 3, totalCount: 10);

        Assert.Equal(4, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_last_page_has_no_next_flag()
    {
        var result = PagedResult<int>.Create([1], page: 4, pageSize: 3, totalCount: 10);

        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }
}
