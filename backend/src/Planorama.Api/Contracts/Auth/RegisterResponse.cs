namespace Planorama.Api.Contracts.Auth;

/// <summary>No tokens here, unlike <see cref="LoginResponse"/> — RequireConfirmedEmail blocks sign-in until the account is confirmed.</summary>
public record RegisterResponse(Guid UserId, string Email);
