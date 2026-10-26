namespace QuickShop.Api.Domain;

/// <summary>A product variant (e.g. storage/RAM or colour option).</summary>
public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Variant label, e.g. "16GB / 512GB" or "Midnight Black".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Delta applied to the product MRP for this variant (can be negative).</summary>
    public decimal PriceDelta { get; set; }

    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
