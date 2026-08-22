namespace Planorama.Core.Jobs;

/// <summary>
/// Runs on a recurring schedule from the Hangfire server in Planorama.Worker — see its
/// Program.cs for the cron registration. Resolves every suggestion whose voting window has
/// closed, or whose accepted trip members have all voted (spec §6.5-6.6). No CancellationToken
/// parameter, matching <see cref="IEmailDispatchJob"/>: a recurring job has no caller-scoped token.
/// </summary>
public interface IVotingResolutionJob
{
    Task ResolveDueSuggestionsAsync();
}
