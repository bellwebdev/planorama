using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Planorama.Core.Exceptions;

namespace Planorama.Api.ErrorHandling;

/// <summary>
/// Maps <see cref="AuthProblemException"/> (and future domain exception hierarchies that follow
/// the same shape) to RFC 7807 problem+json. Anything else falls through to the default
/// UseExceptionHandler() 500 handling — unexpected faults are untouched by this handler.
/// </summary>
public class AuthProblemExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AuthProblemException authException)
        {
            return false;
        }

        httpContext.Response.StatusCode = (int)authException.StatusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = authException,
            ProblemDetails = new ProblemDetails
            {
                Status = (int)authException.StatusCode,
                Title = authException.Title,
                Detail = authException.Detail,
            },
        });
    }
}
