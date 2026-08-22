using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Planorama.Tests.Integration;

/// <summary>Test double for Hangfire's client — job payloads aren't persisted or executed. Records
/// every enqueued <see cref="Job"/> so tests can assert on which jobs/args were dispatched (e.g.
/// suggestion-added recipients) without a real Hangfire server; tests that only need to observe a
/// confirmation email still fetch the token via UserManager directly instead.</summary>
public class NoOpBackgroundJobClient : IBackgroundJobClient
{
    public List<Job> EnqueuedJobs { get; } = [];

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job);
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string? expectedState) => true;
}
