using FluentValidation;
using Planorama.Api.Contracts.Auth;

namespace Planorama.Api.Validation;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
