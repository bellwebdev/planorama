using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>400 — email-confirmation token is invalid, expired, or belongs to a user that no longer exists.</summary>
public class InvalidTokenException()
    : AuthProblemException(HttpStatusCode.BadRequest, "Invalid confirmation token", "The confirmation link is invalid or has expired.");
