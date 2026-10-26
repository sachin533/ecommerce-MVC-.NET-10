using Microsoft.Extensions.Logging.Abstractions;
using QuickShop.Api.Services.Payment;

namespace QuickShop.Tests.Unit;

public class MockPaymentProcessorTests
{
    private static MockPaymentProcessor CreateProcessor()
        => new(NullLogger<MockPaymentProcessor>.Instance);

    private static PaymentRequest Request(string? cardLast4, decimal amount = 500m)
        => new(1, "ORD0000001", amount, "INR", "mock_card", cardLast4);

    [Theory]
    [InlineData("4242")]   // even last digit -> approved
    [InlineData("0000")]
    [InlineData(null)]     // no card on file (e.g. wallet flows) -> approved
    [InlineData("")]
    public async Task Approves_cards_with_an_even_final_digit(string? cardLast4)
    {
        var result = await CreateProcessor().ChargeAsync(Request(cardLast4));

        Assert.True(result.Success);
        Assert.NotNull(result.TransactionId);
        Assert.StartsWith("MOCK-", result.TransactionId);
        Assert.Null(result.FailureReason);
    }

    [Theory]
    [InlineData("4111")]
    [InlineData("0001")]
    [InlineData("9999999999991239")]
    [InlineData("4111111111111111")] // only the final digit decides, even for an over-long value
    public async Task Declines_cards_with_an_odd_final_digit(string cardLast4)
    {
        var result = await CreateProcessor().ChargeAsync(Request(cardLast4));

        Assert.False(result.Success);
        Assert.Null(result.TransactionId);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task Always_rejects_non_positive_amounts()
    {
        var processor = CreateProcessor();

        Assert.False((await processor.ChargeAsync(Request("4242", 0m))).Success);
        Assert.False((await processor.ChargeAsync(Request("4242", -10m))).Success);
    }

    [Fact]
    public async Task Never_returns_real_card_data()
    {
        var result = await CreateProcessor().ChargeAsync(Request("4242"));

        // The provider result only carries an opaque mock transaction id.
        Assert.DoesNotContain("4242", result.TransactionId);
    }
}
