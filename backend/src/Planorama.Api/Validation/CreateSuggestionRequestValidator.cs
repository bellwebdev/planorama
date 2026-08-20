using FluentValidation;
using Planorama.Api.Contracts.Suggestions;

namespace Planorama.Api.Validation;

public class CreateSuggestionRequestValidator : AbstractValidator<CreateSuggestionRequest>
{
    public CreateSuggestionRequestValidator()
    {
        // One of the two creation paths must be usable: a provider place, or a custom title.
        RuleFor(x => x.Title)
            .NotEmpty()
            .When(x => string.IsNullOrWhiteSpace(x.ProviderPlaceId))
            .WithMessage("A title is required when no place is selected.");

        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.ProviderPlaceId).MaximumLength(200);
        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, 1440)
            .When(x => x.DurationMinutes is not null);
    }
}
