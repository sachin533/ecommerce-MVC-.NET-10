using System.ComponentModel.DataAnnotations;
using QuickShop.Api.Common;
using QuickShop.Api.Domain;

namespace QuickShop.Api.DTOs;

public record CheckoutRequest
{
    /// <summary>Use a saved address...</summary>
    public int? AddressId { get; init; }

    /// <summary>...or provide a one-time address.</summary>
    public AddressUpsertRequest? NewAddress { get; init; }

    [Required(ErrorMessage = "Shipping method is required.")]
    [RegularExpression("^(standard|express)$", ErrorMessage = "Shipping method must be standard or express.")]
    public string ShippingMethod { get; init; } = "standard";

    [Required(ErrorMessage = "Payment method is required.")]
    [RegularExpression("^(mock_card)$", ErrorMessage = "Only the mock payment provider is available in this environment.")]
    public string PaymentMethod { get; init; } = "mock_card";

    /// <summary>Only the last 4 digits are ever sent or stored. Never the PAN or CVV.</summary>
    [RegularExpression("^([0-9]{4})?$", ErrorMessage = "Provide only the last 4 digits of the card.")]
    public string? CardLast4 { get; init; }

    [StringLength(1000)]
    public string? CustomerNote { get; init; }
}

public record OrderItemDto
{
    public int Id { get; init; }
    public int? ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? VariantName { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public record PaymentDto
{
    public string Provider { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public string? CardLast4 { get; init; }
    public string? FailureReason { get; init; }
    public decimal Amount { get; init; }
    public DateTime? ProcessedAt { get; init; }
}

public record OrderSummaryDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Total { get; init; }
    public DateTime PlacedAt { get; init; }
    public int ItemCount { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
}

public record OrderDetailDto : OrderSummaryDto
{
    public string ShippingMethod { get; init; } = "standard";
    public string PaymentMethod { get; init; } = "mock_card";
    public string? CustomerNote { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
    public PaymentDto? Payment { get; init; }

    public string ShipFullName { get; init; } = string.Empty;
    public string ShipPhone { get; init; } = string.Empty;
    public string ShipLine1 { get; init; } = string.Empty;
    public string? ShipLine2 { get; init; }
    public string ShipCity { get; init; } = string.Empty;
    public string ShipState { get; init; } = string.Empty;
    public string ShipPostalCode { get; init; } = string.Empty;
    public string ShipCountry { get; init; } = "India";
}

public record CheckoutResponse(OrderDetailDto Order, PaymentDto Payment);

public record OrderStatusUpdateRequest(
    [property: Required(ErrorMessage = "Status is required.")]
    [property: StringLength(32)]
    string Status);
