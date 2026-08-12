namespace Planorama.Api.Contracts.Auth;

public record ConfirmEmailRequest(Guid UserId, string Token);
