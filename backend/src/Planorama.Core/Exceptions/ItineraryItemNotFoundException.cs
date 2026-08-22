using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — the itinerary item doesn't exist, or the caller isn't an accepted member of its trip.</summary>
public class ItineraryItemNotFoundException()
    : AuthProblemException(HttpStatusCode.NotFound, "Itinerary item not found", "This itinerary item doesn't exist or you don't have access to it.");
