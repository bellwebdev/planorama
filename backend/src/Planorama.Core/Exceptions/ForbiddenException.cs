using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>403 — the caller is a known participant (their access to the resource has already
/// been confirmed) but lacks permission for this specific action, e.g. a non-creator trip member
/// attempting a creator-only edit.</summary>
public class ForbiddenException()
    : AuthProblemException(HttpStatusCode.Forbidden, "Forbidden", "You don't have permission to perform this action.");
