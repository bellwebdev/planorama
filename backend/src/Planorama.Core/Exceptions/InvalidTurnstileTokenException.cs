using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>400 — the Cloudflare Turnstile challenge token is missing, invalid, or failed verification.</summary>
public class InvalidTurnstileTokenException()
    : AuthProblemException(HttpStatusCode.BadRequest, "Verification failed", "Please complete the verification challenge and try again.");
