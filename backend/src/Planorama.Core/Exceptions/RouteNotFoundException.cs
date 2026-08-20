using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — the routing provider found no route between the two points for the requested
/// travel mode (e.g. driving across water, or transit where none is mapped).</summary>
public class RouteNotFoundException()
    : AuthProblemException(HttpStatusCode.NotFound, "No route found", "We couldn't find a route there for that travel mode.");
