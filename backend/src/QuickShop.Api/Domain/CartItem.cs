namespace QuickShop.Api.Domain;

/// <summary>A line item in a cart. Prices are never trusted from the client;</summary>
/// <summary>they are re-derived from the product/variant on every read.</summary>
public class CartItem
{
    public int Id { get; set; }

    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
