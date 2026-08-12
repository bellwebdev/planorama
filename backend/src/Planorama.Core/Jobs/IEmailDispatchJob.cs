namespace Planorama.Core.Jobs;

/// <summary>
/// Executed by the Hangfire server in Planorama.Worker only — Planorama.Api enqueues via
/// this interface but never registers an implementation, so email can't be sent inline
/// from a request handler even by mistake. No CancellationToken parameter: Hangfire
/// serializes arguments at enqueue time and runs the job out of band from any request,
/// so a caller-scoped token has no meaning here.
/// </summary>
public interface IEmailDispatchJob
{
    Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationUrl);
}
