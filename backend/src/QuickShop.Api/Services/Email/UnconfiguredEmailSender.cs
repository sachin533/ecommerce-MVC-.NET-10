namespace QuickShop.Api.Services.Email;

/// <summary>
/// Registered outside Development: refuses to pretend that email was delivered and
/// logs loudly so operators wire a real provider (SMTP/SendGrid/etc.).
/// </summary>
public sealed class UnconfiguredEmailSender : IEmailSender
{
    private readonly ILogger<UnconfiguredEmailSender> _logger;

    public UnconfiguredEmailSender(ILogger<UnconfiguredEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(OutboundEmail email, CancellationToken cancellationToken = default)
    {
        _logger.LogError(
            "EMAIL PROVIDER NOT CONFIGURED — message to {To} with subject '{Subject}' was NOT delivered. Configure a real IEmailSender implementation.",
            email.To, email.Subject);
        return Task.CompletedTask;
    }
}
