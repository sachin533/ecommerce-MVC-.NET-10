namespace QuickShop.Api.Services.Email;

public record OutboundEmail(string To, string Subject, string Body);

/// <summary>Absent a real SMTP provider, emails are delivered to an outbox sink.</summary>
public interface IEmailSender
{
    Task SendAsync(OutboundEmail email, CancellationToken cancellationToken = default);
}

/// <summary>
/// DEVELOPMENT-ONLY email sender: writes messages to logs/dev-emails/ and the console
/// instead of pretending an email was delivered. Swap for a real provider (SMTP/SendGrid)
/// in production by registering a different IEmailSender.
/// </summary>
public sealed class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _outboxDirectory;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _outboxDirectory = Path.Combine(environment.ContentRootPath, "logs", "dev-emails");
    }

    public Task SendAsync(OutboundEmail email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL OUTBOX] To: {To} | Subject: {Subject}{NewLine}{Body}",
            email.To, email.Subject, Environment.NewLine, email.Body);

        try
        {
            Directory.CreateDirectory(_outboxDirectory);
            var safeName = string.Join("_", email.To.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}.txt";
            var content =
                $"To: {email.To}\r\nSubject: {email.Subject}\r\nDate: {DateTime.UtcNow:o}\r\n\r\n{email.Body}\r\n";
            File.WriteAllText(Path.Combine(_outboxDirectory, fileName), content);
        }
        catch (Exception ex)
        {
            // The console log above is the primary dev mechanism; file issues are non-fatal.
            _logger.LogWarning(ex, "Could not write dev email file");
        }

        _ = _environment; // environment captured for diagnostics/implicit dev-only marker
        return Task.CompletedTask;
    }
}
