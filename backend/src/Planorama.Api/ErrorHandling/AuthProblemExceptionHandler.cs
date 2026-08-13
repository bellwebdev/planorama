using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Planorama.Core.Exceptions;

namespace Planorama.Api.ErrorHandling;

/// <summary>
/// Maps <see cref="AuthProblemException"/> (and future domain exception hierarchies that follow
/// the same shape) to RFC 7807 problem+json. Also maps <see cref="BadHttpRequestException"/> —
/// the framework's own exception for a request body that fails to even parse as JSON (e.g. an
/// invalid enum string) — to its embedded status code, since otherwise registering this handler
/// overrides ASP.NET Core's normal default of auto-converting a JSON parse failure to 400,
/// producing a 500 instead. Anything else falls through to the default UseExceptionHandler() 500
/// handling — unexpected faults are untouched by this handler.
/// </summary>
public class AuthProblemExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int? statusCode, string? title, string? detail) = exception switch
        {
            AuthProblemException authException => ((int?)(int)authException.StatusCode, authException.Title, (string?)authException.Detail),
            BadHttpRequestException badRequestException => (badRequestException.StatusCode, "Malformed request", badRequestException.Message),
            _ => (null, null, null),
        };

        if (statusCode is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode.Value,
                Title = title,
                Detail = detail,
            },
        });
    }
}
