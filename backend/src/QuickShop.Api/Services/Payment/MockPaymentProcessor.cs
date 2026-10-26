using System.Security.Cryptography;

namespace QuickShop.Api.Services.Payment;

/// <summary>
/// CLEARLY LABELLED MOCK payment provider for local development.
/// It simulates an authorization round-trip and approves every request except:
///   • non-positive amounts (always rejected), and
///   • cards whose last digit is odd (e.g. 4111) — a deterministic, documented
///     decline rule so the payment-failure path (402 → order Cancelled → stock
///     restocked → cart preserved) can be exercised end-to-end in dev and tests.
/// It is NOT a real gateway and must not be used in production.
/// </summary>
public sealed class MockPaymentProcessor : IPaymentProcessor
{
    private readonly ILogger<MockPaymentProcessor> _logger;

    public MockPaymentProcessor(ILogger<MockPaymentProcessor> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "mock";

    public string DisplayName => "Mock Provider (Dev only — simulates approval, no real money moves)";

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Simulated provider latency.
        await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);

        if (request.Amount <= 0)
        {
            _logger.LogWarning("Mock provider rejected order {Order}: non-positive amount {Amount}", request.OrderNumber, request.Amount);
            return new PaymentResult(false, ProviderName, null, "Mock provider rejected non-positive amount.");
        }

        // Documented dev-only decline rule: an odd final digit declines the charge,
        // so the checkout failure path is reproducible (e.g. 4111 declines, 4242 succeeds).
        if (!string.IsNullOrEmpty(request.CardLast4) &&
            int.TryParse(request.CardLast4[^1..], out var lastDigit) &&
            lastDigit % 2 == 1)
        {
            _logger.LogWarning(
                "Mock provider declined order {Order}: card ending {Last4} (odd last digit — simulated decline)",
                request.OrderNumber, request.CardLast4);
            return new PaymentResult(
                false,
                ProviderName,
                null,
                "The payment provider declined the card. (Dev mock: cards whose last digit is odd always decline.)");
        }

        var transactionId = $"MOCK-{Guid.NewGuid():N}"[..22].ToUpperInvariant();
        _logger.LogInformation("Mock provider approved {Amount} {Currency} for order {Order} → {Txn}",
            request.Amount, request.Currency, request.OrderNumber, transactionId);

        return new PaymentResult(true, ProviderName, transactionId, null);
    }
}
