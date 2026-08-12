using FluentValidation;
using Planorama.Api.Contracts.Auth;

namespace Planorama.Api.Validation;

public class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
