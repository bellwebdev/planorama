using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Planorama.Core.Integrations;
using Planorama.Core.Options;
using Planorama.Worker.Options;

namespace Planorama.Worker.Emails;

/// <summary>
/// Production sender: bridges Core's transport-agnostic <see cref="IEmailSender"/> to any standard
/// SMTP provider via MailKit — provider-agnostic by design, so it works with Amazon SES, Brevo, or
/// any other SMTP relay without code changes (only <see cref="SmtpOptions"/> config differs). Chosen
/// over Resend for Planorama specifically because Resend's free tier caps at one verified domain,
/// already spoken for by an unrelated business's lead form. Registered only when not
/// <c>IsDevelopment()</c> — see <see cref="LogOnlyEmailSender"/> for the local alternative.
/// </summary>
public class MailKitSmtpEmailSender(IOptions<SmtpOptions> smtpOptions, IOptions<EmailOptions> emailOptions) : IEmailSender
{
    private readonly SmtpOptions _smtp = smtpOptions.Value;
    private readonly EmailOptions _email = emailOptions.Value;

    /// <inheritdoc/>
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_email.FromName, _email.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.Auto, ct);
        if (!string.IsNullOrEmpty(_smtp.Username))
        {
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
        }
        await client.SendAsync(mimeMessage, ct);
        await client.DisconnectAsync(true, ct);
    }
}
