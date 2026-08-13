namespace Planorama.Core.Profile;

public record ProfileResult(Guid UserId, string Email, string DisplayName, string? AvatarUrl, DateTimeOffset CreatedAt);
