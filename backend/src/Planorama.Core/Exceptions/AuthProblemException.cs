using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>
/// Base for expected, user-facing auth failures. Caught by a single central
/// <c>AuthProblemExceptionHandler</c> in Planorama.Api and mapped straight to RFC 7807
/// problem+json — endpoint handlers never catch these themselves.
/// </summary>
public abstract class AuthProblemException(HttpStatusCode statusCode, string title, string? detail = null)
    : Exception(detail ?? title)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string? Detail { get; } = detail;
}
