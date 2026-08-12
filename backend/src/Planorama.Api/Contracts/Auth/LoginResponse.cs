namespace Planorama.Api.Contracts.Auth;

public record LoginResponse(UserSummary User, TokenPairResponse Tokens);
