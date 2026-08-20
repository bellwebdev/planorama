using FluentValidation;
using Planorama.Api.Contracts.Trips;
using Planorama.Core.Domain;

namespace Planorama.Api.Validation;

public class CreateInviteRequestValidator : AbstractValidator<CreateInviteRequest>
{
    public CreateInviteRequestValidator()
    {
        RuleFor(x => x.Via).IsInEnum();
        RuleFor(x => x.Contact).NotEmpty().EmailAddress().When(x => x.Via == InvitedVia.Email);
    }
}
