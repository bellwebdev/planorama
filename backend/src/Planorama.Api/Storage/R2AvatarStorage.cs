using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Planorama.Api.Options;
using Planorama.Core.Media;

namespace Planorama.Api.Storage;

/// <inheritdoc cref="IAvatarStorage"/>
public class R2AvatarStorage(IAmazonS3 s3Client, IOptions<R2Options> r2Options) : IAvatarStorage
{
    private readonly R2Options _r2 = r2Options.Value;

    public async Task<string> SaveAsync(Guid userId, byte[] bytes, string contentType, CancellationToken ct)
    {
        // Deterministic per-user key: a re-upload overwrites the same object, so there's no
        // orphaned object to clean up (as long as the bucket has versioning left off — the R2
        // default).
        var key = $"avatars/{userId}.jpg";

        using var stream = new MemoryStream(bytes);
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _r2.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false,
            // No CannedACL — R2 doesn't use per-object ACLs like S3; public access is a
            // bucket-level setting (Public Development URL or a custom domain).
            // R2 doesn't implement the SDK's default chunked/streaming SigV4 payload signing
            // (STREAMING-AWS4-HMAC-SHA256-PAYLOAD) — fall back to signing the whole payload
            // as a single, non-chunked signature instead.
            UseChunkEncoding = false,
        }, ct);

        // Cache-busting query param: the object key is stable, but this string changes on every
        // upload, so clients/CDNs never serve a stale cached image for the same key.
        return $"{_r2.PublicBaseUrl.TrimEnd('/')}/{key}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
