using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>423 — ASP.NET Identity's own lockout tripped (too many recent failed sign-in attempts).</summary>
public class AccountLockedException()
    : AuthProblemException(HttpStatusCode.Locked, "Account locked", "Too many failed sign-in attempts. Try again later.");
