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

    /// <inheritdoc/>
    public Task SendSuggestionAddedAsync(string toEmail, string recipientName, string tripName, string suggestionTitle, string tripUrl)
    {
        var html = $"""
            <p>Hi {recipientName},</p>
            <p><strong>{suggestionTitle}</strong> was just suggested for <strong>{tripName}</strong>.</p>
            <p><a href="{tripUrl}">View and vote</a></p>
            """;
        var text = $"Hi {recipientName},\n\n{suggestionTitle} was just suggested for {tripName}.\n\nView and vote: {tripUrl}";

        var message = new EmailMessage(toEmail, $"New suggestion for {tripName}", html, text);
        return emailSender.SendAsync(message, CancellationToken.None); // No request-scoped token exists inside a background job.
    }

    /// <inheritdoc/>
    public Task SendVoteResultAsync(string toEmail, string recipientName, string tripName, string suggestionTitle, bool approved, string tripUrl)
    {
        var outcome = approved ? "approved" : "not approved";
        var html = $"""
            <p>Hi {recipientName},</p>
            <p><strong>{suggestionTitle}</strong> was {outcome} for <strong>{tripName}</strong>.</p>
            <p><a href="{tripUrl}">View the trip</a></p>
            """;
        var text = $"Hi {recipientName},\n\n{suggestionTitle} was {outcome} for {tripName}.\n\nView the trip: {tripUrl}";

        var message = new EmailMessage(toEmail, $"Voting result for {suggestionTitle}", html, text);
        return emailSender.SendAsync(message, CancellationToken.None); // No request-scoped token exists inside a background job.
    }

    /// <inheritdoc/>
    public Task SendEventReminderAsync(string toEmail, string recipientName, string tripName, string itemTitle, string tripUrl)
    {
        var html = $"""
            <p>Hi {recipientName},</p>
            <p><strong>{itemTitle}</strong> on <strong>{tripName}</strong> is coming up.</p>
            <p><a href="{tripUrl}">View the itinerary</a></p>
            """;
        var text = $"Hi {recipientName},\n\n{itemTitle} on {tripName} is coming up.\n\nView the itinerary: {tripUrl}";

        var message = new EmailMessage(toEmail, $"Reminder: {itemTitle}", html, text);
        return emailSender.SendAsync(message, CancellationToken.None); // No request-scoped token exists inside a background job.
    }
}
