namespace Planorama.Api.Contracts.Me;

public record MeResponse(Guid Id, string Email, string DisplayName, string? AvatarUrl, DateTimeOffset CreatedAt);
