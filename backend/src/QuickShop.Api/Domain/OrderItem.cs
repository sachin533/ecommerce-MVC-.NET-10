namespace QuickShop.Api.Domain;

/// <summary>
/// An order line with price/name snapshots taken at purchase time so historical
/// orders remain accurate even if the product later changes or is removed.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>Set null when the original product is deleted (snapshot survives).</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Variant FK (detached if the variant is removed later; name is snapshotted).</summary>
    public int? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string? VariantName { get; set; }

    /// <summary>Unit price actually charged (after discount/variant delta).</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>UnitPrice * Quantity.</summary>
    public decimal LineTotal { get; set; }
}
