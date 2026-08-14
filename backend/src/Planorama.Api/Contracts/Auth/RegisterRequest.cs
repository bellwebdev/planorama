namespace Planorama.Api.Contracts.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName, string TurnstileToken);
