using FluentValidation;

namespace Planorama.Api.Filters;

/// <summary>
/// Generic endpoint filter, one instance per request DTO type (e.g. <c>AddEndpointFilter&lt;ValidationFilter&lt;RegisterRequest&gt;&gt;()</c>).
/// Resolves <see cref="IValidator{T}"/> from DI and rejects with <c>Results.ValidationProblem</c>
/// before the handler runs — the handler never sees an invalid request.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var result = await validator.ValidateAsync(argument);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
