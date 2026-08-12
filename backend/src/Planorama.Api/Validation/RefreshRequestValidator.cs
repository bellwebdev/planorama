using FluentValidation;
using Planorama.Api.Contracts.Auth;

namespace Planorama.Api.Validation;

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
