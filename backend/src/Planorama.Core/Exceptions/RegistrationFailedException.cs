using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>400 — catch-all for Identity <c>CreateAsync</c> failures other than a duplicate email/username (e.g. password-policy rejection Identity itself caught). Should rarely trigger since FluentValidation already enforces the same password policy at the boundary.</summary>
public class RegistrationFailedException(string detail)
    : AuthProblemException(HttpStatusCode.BadRequest, "Registration failed", detail);
