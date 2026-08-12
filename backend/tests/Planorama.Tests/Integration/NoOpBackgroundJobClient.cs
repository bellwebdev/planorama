using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Planorama.Tests.Integration;

/// <summary>Test double for Hangfire's client — job payloads aren't persisted or executed; tests that need to observe a dispatched email fetch the confirmation token via UserManager directly instead.</summary>
public class NoOpBackgroundJobClient : IBackgroundJobClient
{
    public string Create(Job job, IState state) => Guid.NewGuid().ToString();

    public bool ChangeState(string jobId, IState state, string? expectedState) => true;
}
