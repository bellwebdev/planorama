using Microsoft.Extensions.Options;
using Planorama.Core.Options;
using Resend;

namespace Planorama.Worker.Emails;

/// <summary>
/// Production sender: bridges Core's transport-agnostic <see cref="Planorama.Core.Integrations.IEmailSender"/>
/// to the Resend SDK. Business logic (AuthService) never references <see cref="IResend"/> directly.
/// Registered only when not <c>IsDevelopment()</c> — see <c>LogOnlyEmailSender</c> for the local alternative.
/// </summary>
public class ResendEmailSender(IResend resend, IOptions<EmailOptions> emailOptions) : Planorama.Core.Integrations.IEmailSender
{
    private readonly EmailOptions _email = emailOptions.Value;

    /// <inheritdoc cref="Planorama.Core.Integrations.IEmailSender.SendAsync"/>
    public Task SendAsync(Planorama.Core.Integrations.EmailMessage message, CancellationToken ct)
    {
        var resendMessage = new global::Resend.EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromAddress}>",
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };
        resendMessage.To.Add(message.To);

        return resend.EmailSendAsync(resendMessage, ct);
    }
}
