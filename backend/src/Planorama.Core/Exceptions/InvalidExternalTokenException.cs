using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>401 — the external provider's sign-in token failed signature/issuer/audience/expiry verification.</summary>
public class InvalidExternalTokenException()
    : AuthProblemException(HttpStatusCode.Unauthorized, "Invalid sign-in token", "The sign-in token could not be verified.");
