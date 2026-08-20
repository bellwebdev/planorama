using Planorama.Core.Domain;

namespace Planorama.Core.Trips;

public record InviteResult(Guid Token, InvitedVia InvitedVia, string? Contact, DateTimeOffset ExpiresAt);
