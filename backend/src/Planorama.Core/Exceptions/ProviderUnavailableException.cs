using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>502 — the third-party provider errored, timed out, or returned something unparseable.</summary>
public class ProviderUnavailableException(string? detail = null, Exception? innerException = null)
    : AuthProblemException(
        HttpStatusCode.BadGateway,
        "Place provider unavailable",
        detail ?? "The place search service didn't respond. Please try again.",
        innerException);
