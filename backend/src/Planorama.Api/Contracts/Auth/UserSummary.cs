namespace Planorama.Api.Contracts.Auth;

public record UserSummary(Guid Id, string Email, string DisplayName, string? AvatarUrl);
