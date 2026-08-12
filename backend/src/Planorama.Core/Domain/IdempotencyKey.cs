namespace Planorama.Core.Domain;

/// <summary>Dedup record for an `Idempotency-Key` header on an email-sending POST; rows expire and are purge-eligible after <see cref="ExpiresAt"/>.</summary>
public class IdempotencyKey
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}
