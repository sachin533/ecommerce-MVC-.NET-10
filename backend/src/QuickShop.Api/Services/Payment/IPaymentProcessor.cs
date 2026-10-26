namespace QuickShop.Api.Services.Payment;

/// <summary>Provider-agnostic charge request. Never contains card numbers or CVVs.</summary>
public record PaymentRequest(
    int OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string Method,
    string? CardLast4);

/// <summary>Provider result. A payment only "succeeded" if the provider says so.</summary>
public record PaymentResult(
    bool Success,
    string Provider,
    string? TransactionId,
    string? FailureReason);

/// <summary>
/// Payment provider abstraction. Real gateways (Stripe, Razorpay, ...) implement this
/// and are registered instead of the mock via configuration — the mock is clearly
/// labelled and intended for local development only.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>Provider key, e.g. "mock".</summary>
    string ProviderName { get; }

    /// <summary>Human-readable label surfaced to clients, e.g. "Mock Provider (Dev)".</summary>
    string DisplayName { get; }

    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}
