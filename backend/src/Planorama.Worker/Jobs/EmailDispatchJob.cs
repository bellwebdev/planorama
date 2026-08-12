using Planorama.Core.Integrations;
using Planorama.Core.Jobs;

namespace Planorama.Worker.Jobs;

/// <summary>
/// Owns the confirmation-email template (subject/HTML/text). Api only sends the confirmation
/// URL — keeping the template here means it can change without redeploying Api, and keeps
/// Hangfire job payloads small.
/// </summary>
public class EmailDispatchJob(IEmailSender emailSender) : IEmailDispatchJob
{
    /// <inheritdoc/>
    public Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationUrl)
    {
        var html = $"""
            <p>Hi {displayName},</p>
            <p>Confirm your email address to finish setting up your Planorama account:</p>
            <p><a href="{confirmationUrl}">Confirm your email</a></p>
            <p>If you didn't create this account, you can ignore this email.</p>
            """;
        var text = $"Hi {displayName},\n\nConfirm your email address: {confirmationUrl}\n\nIf you didn't create this account, you can ignore this email.";

        var message = new EmailMessage(toEmail, "Confirm your Planorama account", html, text);
        return emailSender.SendAsync(message, CancellationToken.None); // No request-scoped token exists inside a background job.
    }
}
