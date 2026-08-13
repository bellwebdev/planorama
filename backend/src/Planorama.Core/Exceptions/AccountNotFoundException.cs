using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>404 — a validated JWT's subject no longer maps to an existing account (e.g. deleted mid-session).</summary>
public class AccountNotFoundException()
    : AuthProblemException(HttpStatusCode.NotFound, "Account not found", "This account no longer exists.");
