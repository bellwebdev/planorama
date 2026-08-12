namespace Planorama.Core.Auth;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);
