using System.ComponentModel.DataAnnotations;
using QuickShop.Api.Domain;

namespace QuickShop.Api.DTOs;

public record AddToCartRequest(
    [property: Range(1, int.MaxValue, ErrorMessage = "A product is required.")]
    int ProductId,

    int? VariantId,

    [property: Range(1, Pricing.MaxCartQuantity, ErrorMessage = "Quantity must be between 1 and 20.")]
    int Quantity = 1);

public record UpdateCartItemRequest(
    [property: Range(1, Pricing.MaxCartQuantity, ErrorMessage = "Quantity must be between 1 and 20.")]
    int Quantity);

public record CartItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public int? VariantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string? VariantName { get; init; }

    /// <summary>Server-computed unit price (after discount and variant delta).</summary>
    public decimal UnitPrice { get; init; }

    public decimal Mrp { get; init; }
    public decimal DiscountPercent { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
    public int Stock { get; init; }
    public int MaxQuantity { get; init; }
    public bool IsAvailable { get; init; }
}

public record CartDto
{
    public Guid Id { get; init; }
    public string Owner { get; init; } = "guest"; // "guest" | "user"
    public IReadOnlyList<CartItemDto> Items { get; init; } = Array.Empty<CartItemDto>();
    public int ItemCount { get; init; }
    public decimal Subtotal { get; init; }
    public decimal EstimatedShipping { get; init; }
    public decimal Total { get; init; }
    public bool IsEmpty => Items.Count == 0;
}
