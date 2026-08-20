using FluentValidation;
using Planorama.Api.Contracts.Places;

namespace Planorama.Api.Validation;

public class PlaceSearchRequestValidator : AbstractValidator<PlaceSearchRequest>
{
    public PlaceSearchRequestValidator()
    {
        RuleFor(x => x.Category).NotNull().IsInEnum();
        RuleFor(x => x.Radius)
            .InclusiveBetween(PlaceSearchRequest.MinRadiusMeters, PlaceSearchRequest.MaxRadiusMeters)
            .When(x => x.Radius is not null);
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, PlaceSearchRequest.MaxLimit)
            .When(x => x.Limit is not null);
        RuleFor(x => x.Q).MaximumLength(PlaceSearchRequest.MaxNameFilterLength);
    }
}
