namespace Planorama.Core.Auth;

public record LoginResult(Guid UserId, string Email, string DisplayName, string? AvatarUrl, TokenPair Tokens);
