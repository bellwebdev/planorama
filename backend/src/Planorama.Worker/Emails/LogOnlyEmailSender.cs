using Microsoft.Extensions.Logging;
using Planorama.Core.Integrations;

namespace Planorama.Worker.Emails;

/// <summary>
/// Development-only IEmailSender: logs instead of calling Resend so `docker compose up`
/// works locally without a live API key, and confirmation links can be read from logs.
/// </summary>
public class LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger) : IEmailSender
{
    /// <inheritdoc/>
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation(
            "Email (not sent — Development mode) To={To} Subject={Subject}\n{HtmlBody}",
            message.To, message.Subject, message.HtmlBody);
        return Task.CompletedTask;
    }
}
