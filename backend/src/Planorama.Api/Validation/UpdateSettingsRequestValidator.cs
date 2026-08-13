using FluentValidation;
using Planorama.Api.Contracts.Me;

namespace Planorama.Api.Validation;

public class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.ReminderOffset).IsInEnum();
    }
}
