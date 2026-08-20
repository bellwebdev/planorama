using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — the suggestion doesn't exist, or the caller isn't an accepted member of its trip.
/// Collapsing both avoids leaking a suggestion's existence to non-members.</summary>
public class SuggestionNotFoundException()
    : AuthProblemException(HttpStatusCode.NotFound, "Suggestion not found", "This suggestion doesn't exist or you don't have access to it.");
