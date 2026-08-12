using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>403 — credentials were valid but the account's email isn't confirmed yet.</summary>
public class EmailNotConfirmedException()
    : AuthProblemException(HttpStatusCode.Forbidden, "Email not confirmed", "Confirm your email address before signing in.");
