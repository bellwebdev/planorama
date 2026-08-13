using Planorama.Core.Media;

namespace Planorama.Tests.Integration;

/// <summary>Test double for R2 — no network calls; returns a deterministic fake URL that changes per call (mirrors R2AvatarStorage's cache-busting query param).</summary>
public class FakeAvatarStorage : IAvatarStorage
{
    public Task<string> SaveAsync(Guid userId, byte[] bytes, string contentType, CancellationToken ct) =>
        Task.FromResult($"https://fake-avatars.test/{userId}.jpg?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
}
