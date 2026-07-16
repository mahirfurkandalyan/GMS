namespace Gms.Api.Services.Notifications;

/// <summary>An outgoing email message (rendered).</summary>
public sealed record EmailMessage(string ToEmail, string ToName, string Subject, string Body);

/// <summary>
/// Thrown by an email provider for a PERMANENT failure (e.g. invalid recipient, rejected content)
/// that must NOT be retried — the delivery worker dead-letters it immediately. Any other failure
/// (return false / other exception) is treated as transient and retried with backoff.
/// </summary>
public sealed class EmailPermanentException : Exception
{
    public EmailPermanentException(string message) : base(message) { }
}

/// <summary>
/// Email channel abstraction. Swapping to a real SMTP/SendGrid provider later means one
/// implementation, with no changes to NotificationService.
/// </summary>
public interface IEmailProvider
{
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>
/// Development email provider — LOGS outgoing mail and never actually sends. No SMTP.
/// Always reports success so delivery records are populated end-to-end.
/// </summary>
public sealed class DummyEmailProvider : IEmailProvider
{
    private readonly ILogger<DummyEmailProvider> _logger;

    public DummyEmailProvider(ILogger<DummyEmailProvider> logger) => _logger = logger;

    public Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        // Never send real mail; just log (no sensitive data beyond the rendered notification).
        _logger.LogInformation("[DUMMY-EMAIL] To: {Email} ({Name}) | Subject: {Subject}",
            message.ToEmail, message.ToName, message.Subject);
        return Task.FromResult(true);
    }
}
