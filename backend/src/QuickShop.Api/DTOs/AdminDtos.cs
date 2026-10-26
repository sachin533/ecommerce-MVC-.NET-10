using System.ComponentModel.DataAnnotations;

namespace QuickShop.Api.DTOs;

public record AdminProductUpsertRequest
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "Name must be 3-300 characters.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(120, MinimumLength = 2)]
    public string Brand { get; init; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters.")]
    public string Description { get; init; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(1, 10_000_000, ErrorMessage = "Price must be between ₹1 and ₹1,00,00,000.")]
    public decimal Price { get; init; }

    [Range(0, 90, ErrorMessage = "Discount must be between 0 and 90%.")]
    public decimal DiscountPercent { get; init; }

    [Range(0, 1_000_000, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; init; }

    [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5.")]
    public decimal Rating { get; init; }

    [Range(0, 1_000_000)]
    public int RatingCount { get; init; }

    [Required(ErrorMessage = "Category is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    public int CategoryId { get; init; }

    [Required(ErrorMessage = "Image URL is required.")]
    [StringLength(1000)]
    public string ImageUrl { get; init; } = string.Empty;

    [StringLength(64)]
    public string? Sku { get; init; }

    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>Optional extra gallery image URLs.</summary>
    [MaxLength(8, ErrorMessage = "At most 8 gallery images.")]
    public List<string>? Images { get; init; }

    public List<VariantUpsertRequest>? Variants { get; init; }
}

public record VariantUpsertRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(-1_000_000, 1_000_000)]
    public decimal PriceDelta { get; init; }

    [Range(0, 1_000_000)]
    public int Stock { get; init; }

    public bool IsActive { get; set; } = true;
}

public record InventoryUpdateRequest(
    [property: Range(0, 1_000_000, ErrorMessage = "Stock cannot be negative.")]
    int Stock);

public record CustomerDto
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public int OrderCount { get; init; }
    public decimal TotalSpent { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public record RevenuePointDto(DateOnly Date, decimal Revenue, int Orders);

public record CategoryRevenueDto(string Name, decimal Revenue, int Orders);

public record LowStockItemDto(int Id, string Name, string Sku, int Stock, string CategoryName);

public record DashboardStatsDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalCustomers { get; init; }
    public int TotalProducts { get; init; }
    public int PendingOrders { get; init; }
    public int LowStockProducts { get; init; }
    public IReadOnlyList<RevenuePointDto> RevenueLast7Days { get; init; } = Array.Empty<RevenuePointDto>();
    public IReadOnlyList<CategoryRevenueDto> TopCategories { get; init; } = Array.Empty<CategoryRevenueDto>();
    public IReadOnlyList<OrderSummaryDto> RecentOrders { get; init; } = Array.Empty<OrderSummaryDto>();
    public IReadOnlyList<LowStockItemDto> LowStockItems { get; init; } = Array.Empty<LowStockItemDto>();
}
