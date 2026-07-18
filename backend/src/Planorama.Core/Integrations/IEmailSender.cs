namespace Planorama.Core.Integrations;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody = null);
