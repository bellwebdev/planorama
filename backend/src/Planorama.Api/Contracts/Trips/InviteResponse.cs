using Planorama.Core.Domain;

namespace Planorama.Api.Contracts.Trips;

public record InviteResponse(Guid Token, InvitedVia InvitedVia, string? Contact, DateTimeOffset ExpiresAt);
