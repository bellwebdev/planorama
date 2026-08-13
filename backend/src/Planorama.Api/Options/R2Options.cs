namespace Planorama.Api.Options;

public class R2Options
{
    public const string SectionName = "R2";

    /// <summary>Cloudflare account ID — builds the R2 S3-compatible endpoint https://{AccountId}.r2.cloudflarestorage.com.</summary>
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Public base URL avatars are served from — the bucket's public R2.dev URL or a custom domain fronting it.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
