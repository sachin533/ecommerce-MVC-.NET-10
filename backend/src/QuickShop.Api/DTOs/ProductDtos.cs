using System.ComponentModel.DataAnnotations;

namespace QuickShop.Api.DTOs;

/// <summary>Server-side product listing query (all filtering happens on the server).</summary>
public class ProductQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Nullable so the minimal-API [AsParameters] binder treats them as optional.
    public bool? InStockOnly { get; set; }
    public bool? Featured { get; set; }

    /// <summary>newest | price-asc | price-desc | rating | popularity</summary>
    public string? Sort { get; set; }

    public int? Page { get; set; }
    public int? PageSize { get; set; }

    /// <summary>Admin only: include inactive products.</summary>
    public bool? IncludeInactive { get; set; }
}

public record ProductImageDto(int Id, string Url, int SortOrder);

public record ProductVariantDto(
    int Id,
    string Name,
    decimal PriceDelta,
    decimal EffectivePrice,
    int Stock,
    bool IsAvailable);

public record ProductSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal SalePrice { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public decimal Rating { get; init; }
    public int RatingCount { get; init; }
    public int Stock { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ProductDetailDto : ProductSummaryDto
{
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();
    public IReadOnlyList<ProductVariantDto> Variants { get; init; } = Array.Empty<ProductVariantDto>();
}

public record PriceRangeDto(decimal Min, decimal Max);

public record CategoryFacetDto(int Id, string Name, string Slug, int ProductCount);

/// <summary>Facets powering the shop filter sidebar.</summary>
public record ProductFiltersDto(
    IReadOnlyList<string> Brands,
    PriceRangeDto PriceRange,
    IReadOnlyList<CategoryFacetDto> Categories,
    int TotalProducts);
