namespace QuickShop.Api.Domain;

/// <summary>Payment record for an order. Never stores card numbers or CVVs.</summary>
public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    /// <summary>Payment provider key, e.g. "mock".</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider-side transaction/reference id (never card data).</summary>
    public string? ProviderTransactionId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? FailureReason { get; set; }

    /// <summary>Only last-4 digits are ever retained, never the full PAN or CVV.</summary>
    public string? CardLast4 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
