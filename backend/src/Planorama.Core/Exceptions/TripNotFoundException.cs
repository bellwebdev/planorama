using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — the trip doesn't exist, or the caller isn't an accepted member. Collapsing both
/// cases into the same response avoids leaking a trip's existence to non-members.</summary>
public class TripNotFoundException()
    : AuthProblemException(HttpStatusCode.NotFound, "Trip not found", "This trip doesn't exist or you don't have access to it.");
