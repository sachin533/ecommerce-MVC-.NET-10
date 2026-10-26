namespace QuickShop.Api.Domain;

/// <summary>
/// Central pricing rules. Every price shown or charged by the application is derived
/// here from database values — client-submitted prices are never trusted.
/// </summary>
public static class Pricing
{
    public const decimal StandardShippingCost = 49m;
    public const decimal ExpressShippingCost = 199m;
    public const decimal FreeShippingThreshold = 999m;

    public const int MaxCartQuantity = 20;

    /// <summary>MRP minus discount, rounded to whole rupees (Indian retail style).</summary>
    public static decimal EffectivePrice(decimal mrp, decimal discountPercent)
    {
        var discount = Math.Clamp(discountPercent, 0m, 90m);
        var value = mrp * (100m - discount) / 100m;
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>Unit price for a product+variant combination before quantity is applied.</summary>
    public static decimal UnitPrice(Product product, ProductVariant? variant)
        => EffectivePrice(product.Price + (variant?.PriceDelta ?? 0m), product.DiscountPercent);

    /// <summary>Shipping cost for a method given the cart subtotal.</summary>
    public static decimal ShippingCost(ShippingMethod method, decimal subtotal) => method switch
    {
        // Free standard shipping above the threshold.
        ShippingMethod.Standard => subtotal >= FreeShippingThreshold ? 0m : StandardShippingCost,
        ShippingMethod.Express => ExpressShippingCost,
        _ => StandardShippingCost
    };
}
