using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>409 — thrown by <c>AuthService.RegisterAsync</c> when the email is already registered.</summary>
public class EmailAlreadyRegisteredException()
    : AuthProblemException(HttpStatusCode.Conflict, "Email already registered", "An account with this email address already exists.");
