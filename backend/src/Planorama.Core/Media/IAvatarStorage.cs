namespace Planorama.Core.Media;

/// <summary>
/// Seam between Core and the object-storage SDK: Core depends only on this interface, never on
/// AWSSDK.S3 directly. Implemented in Planorama.Api (<c>R2AvatarStorage</c>), which owns the R2
/// credentials and the AWS SDK package.
/// </summary>
public interface IAvatarStorage
{
    /// <summary>Persists already-processed avatar bytes and returns the resulting public URL.</summary>
    Task<string> SaveAsync(Guid userId, byte[] bytes, string contentType, CancellationToken ct);
}
