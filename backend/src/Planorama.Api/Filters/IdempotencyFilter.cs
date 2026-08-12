using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planorama.Core.Data;
using Planorama.Core.Domain;

namespace Planorama.Api.Filters;

/// <summary>
/// Requires an `Idempotency-Key` header and dedups on (endpoint, key) via a small Postgres
/// table. Only a genuine 2xx success permanently consumes the key — failed attempts (validation
/// errors, thrown AuthProblemExceptions, etc.) remove the row again so the client can retry.
/// </summary>
public class IdempotencyFilter(string endpoint) : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headerValue = context.HttpContext.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Idempotency-Key header is required");
        }

        var ct = context.HttpContext.RequestAborted;
        var db = context.HttpContext.RequestServices.GetRequiredService<PlanoramaDbContext>();

        // Kept so a later removal reuses this same tracked instance — attaching a second
        // instance with the same key would make EF's change tracker throw a conflict.
        var idempotencyKey = new IdempotencyKey
        {
            Endpoint = endpoint,
            Key = headerValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
        };
        db.IdempotencyKeys.Add(idempotencyKey);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate request", detail: "This request has already been submitted.");
        }

        object? result;
        try
        {
            result = await next(context);
        }
        catch
        {
            db.IdempotencyKeys.Remove(idempotencyKey);
            await db.SaveChangesAsync(ct);
            throw;
        }

        if (result is IStatusCodeHttpResult { StatusCode: >= 400 })
        {
            db.IdempotencyKeys.Remove(idempotencyKey);
            await db.SaveChangesAsync(ct);
        }

        return result;
    }
}
