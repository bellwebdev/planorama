namespace Planorama.Core.Auth;

public record LoginResult(Guid UserId, string Email, string DisplayName, TokenPair Tokens);
