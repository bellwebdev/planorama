using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — the invite token doesn't exist or has expired.</summary>
public class InvalidInviteTokenException()
    : AuthProblemException(HttpStatusCode.NotFound, "Invite not found", "This invite link is invalid or has expired.");
