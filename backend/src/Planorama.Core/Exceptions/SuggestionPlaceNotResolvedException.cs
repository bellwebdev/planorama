using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>400 — a suggestion needs either a resolvable provider place or a title, and neither
/// was usable.</summary>
public class SuggestionPlaceNotResolvedException(string detail)
    : AuthProblemException(HttpStatusCode.BadRequest, "Suggestion place couldn't be resolved", detail);
