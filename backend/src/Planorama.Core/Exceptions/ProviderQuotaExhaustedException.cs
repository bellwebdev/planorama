using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>503 — today's third-party API allowance is spent and the request wasn't served from
/// cache. Transient by definition: the counter resets at UTC midnight.</summary>
public class ProviderQuotaExhaustedException()
    : AuthProblemException(
        HttpStatusCode.ServiceUnavailable,
        "Search temporarily limited",
        "Place search has hit its daily limit. Cached results still work, and the limit resets shortly.");
