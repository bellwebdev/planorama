using FluentValidation;
using Planorama.Api.Contracts.Trips;

namespace Planorama.Api.Validation;

public class UpdateTripRequestValidator : AbstractValidator<UpdateTripRequest>
{
    public UpdateTripRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StayAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.DefaultVotingWindowHours).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
    }
}
