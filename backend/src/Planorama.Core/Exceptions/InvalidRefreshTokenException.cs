using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>401 — refresh token is unknown, expired, or was already rotated/revoked. Covers the reuse-detection case too: replaying an already-rotated token throws this after revoking the whole token family.</summary>
public class InvalidRefreshTokenException()
    : AuthProblemException(HttpStatusCode.Unauthorized, "Invalid refresh token", "The refresh token is invalid, expired, or has already been used.");
