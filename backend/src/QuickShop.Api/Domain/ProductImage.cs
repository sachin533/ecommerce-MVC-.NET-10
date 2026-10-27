namespace QuickShop.Api.Domain;

/// <summary>An image belonging to a product gallery.</summary>
public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Url { get; set; } = string.Empty;

    /// <summary>Accessible description of the image ("MacBook Air M3 front view").</summary>
    public string AltText { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>True for the primary gallery image (mirrors <see cref="Product.ImageUrl"/>).</summary>
    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
