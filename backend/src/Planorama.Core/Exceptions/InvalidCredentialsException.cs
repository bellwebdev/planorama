using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>401 — unknown email or wrong password. Same exception for both cases, deliberately, so the response can't be used to enumerate accounts.</summary>
public class InvalidCredentialsException()
    : AuthProblemException(HttpStatusCode.Unauthorized, "Invalid credentials", "The email or password is incorrect.");
