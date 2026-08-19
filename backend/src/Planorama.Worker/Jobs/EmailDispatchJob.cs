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

    /// <inheritdoc/>
    public Task SendTripInviteAsync(string toEmail, string tripName, string acceptUrl)
    {
        var html = $"""
            <p>You've been invited to join <strong>{tripName}</strong> on Planorama.</p>
            <p><a href="{acceptUrl}">View the trip</a></p>
            <p>If you don't have a Planorama account yet, you'll be asked to create one first.</p>
            """;
        var text = $"You've been invited to join {tripName} on Planorama: {acceptUrl}\n\nIf you don't have a Planorama account yet, you'll be asked to create one first.";

        var message = new EmailMessage(toEmail, $"You're invited to {tripName}", html, text);
        return emailSender.SendAsync(message, CancellationToken.None); // No request-scoped token exists inside a background job.
    }
}
