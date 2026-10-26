namespace QuickShop.Api.Domain;

/// <summary>A customer order with an immutable shipping-address snapshot.</summary>
public class Order
{
    public int Id { get; set; }

    /// <summary>Human-friendly order number, e.g. "ORD0001234".</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Sum of item line totals at the time of purchase.</summary>
    public decimal Subtotal { get; set; }

    public decimal ShippingCost { get; set; }

    /// <summary>Subtotal + shipping. Denormalised total kept in sync on write.</summary>
    public decimal Total { get; set; }

    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;

    /// <summary>Payment method key, e.g. "mock_card".</summary>
    public string PaymentMethod { get; set; } = "mock_card";

    // Shipping address snapshot (kept even if the user later edits/deletes the address).
    public string ShipFullName { get; set; } = string.Empty;
    public string ShipPhone { get; set; } = string.Empty;
    public string ShipLine1 { get; set; } = string.Empty;
    public string? ShipLine2 { get; set; }
    public string ShipCity { get; set; } = string.Empty;
    public string ShipState { get; set; } = string.Empty;
    public string ShipPostalCode { get; set; } = string.Empty;
    public string ShipCountry { get; set; } = "India";

    public string? CustomerNote { get; set; }

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}
