using FluentValidation;
using Planorama.Api.Contracts.Places;

namespace Planorama.Api.Validation;

public class RouteRequestValidator : AbstractValidator<RouteRequest>
{
    public RouteRequestValidator()
    {
        RuleFor(x => x.ToLat).NotNull().InclusiveBetween(-90, 90);
        RuleFor(x => x.ToLng).NotNull().InclusiveBetween(-180, 180);
        RuleFor(x => x.Mode).IsInEnum().When(x => x.Mode is not null);
    }
}
