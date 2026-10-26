namespace QuickShop.Api.Common;

/// <summary>Base type for errors that map to HTTP status codes in the exception handler.</summary>
public abstract class ApiException : Exception
{
    public abstract int StatusCode { get; }
    public string Title { get; }

    protected ApiException(string title, string message) : base(message)
    {
        Title = title;
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string resource)
        : base("Resource not found", $"{resource} was not found.")
    {
    }

    public NotFoundException(string resource, string message)
        : base("Resource not found", message)
    {
    }

    public override int StatusCode => 404;
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message)
        : base("Conflict", message)
    {
    }

    public override int StatusCode => 409;
}

/// <summary>Business-rule validation failure carrying field-level errors.</summary>
public sealed class ValidationException : ApiException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base("Validation failed", message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public override int StatusCode => 400;
}

/// <summary>Authentication/authorization semantic error (e.g. invalid credentials).</summary>
public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message)
        : base("Unauthorized", message)
    {
    }

    public override int StatusCode => 401;
}

/// <summary>Payment provider declined the transaction.</summary>
public sealed class PaymentFailedException : ApiException
{
    public string OrderNumber { get; }
    public string? ProviderReason { get; }

    public PaymentFailedException(string orderNumber, string? providerReason)
        : base("Payment failed", "The payment could not be completed. Your order was not confirmed.")
    {
        OrderNumber = orderNumber;
        ProviderReason = providerReason;
    }

    public override int StatusCode => 402;
}
