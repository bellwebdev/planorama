using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — the provider has no place with the requested identifier.</summary>
public class PlaceNotFoundException()
    : AuthProblemException(HttpStatusCode.NotFound, "Place not found", "We couldn't find that place.");
