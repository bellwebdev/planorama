using Planorama.Core.Domain;

namespace Planorama.Api.Contracts.Trips;

public record CreateInviteRequest(InvitedVia Via, string? Contact);
