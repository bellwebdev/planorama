using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>409 — the trip has no resolved stay coordinate, so there's no origin to search or route
/// from. Recoverable by the creator editing the stay address into something geocodable.</summary>
public class TripNotGeocodedException()
    : AuthProblemException(
        HttpStatusCode.Conflict,
        "Trip location not resolved",
        "We couldn't pin this trip's stay address on the map. Ask the trip creator to edit it into a more complete address.");
