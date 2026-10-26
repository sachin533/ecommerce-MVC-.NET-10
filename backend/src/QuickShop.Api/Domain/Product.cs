using System.ComponentModel.DataAnnotations.Schema;

namespace QuickShop.Api.Domain;

/// <summary>A sellable product in the catalog.</summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Maximum retail price (MRP) in INR.</summary>
    public decimal Price { get; set; }

    /// <summary>Discount percentage off the MRP (0-90).</summary>
    public decimal DiscountPercent { get; set; }

    public int Stock { get; set; }

    /// <summary>Aggregate rating 0-5 (seeded demo metric, no fabricated reviews).</summary>
    public decimal Rating { get; set; }

    public int RatingCount { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Primary image URL used in listing grids.</summary>
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Availability = listed and in stock.</summary>
    [NotMapped]
    public bool IsAvailable => IsActive && Stock > 0;

    /// <summary>Price after discount, before variant adjustments (rounded to whole rupees).</summary>
    [NotMapped]
    public decimal EffectivePrice => Pricing.EffectivePrice(Price, DiscountPercent);
}
