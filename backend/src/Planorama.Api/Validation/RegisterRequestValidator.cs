using FluentValidation;
using Planorama.Api.Contracts.Auth;

namespace Planorama.Api.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        // Mirrors IdentityOptions.Password set in Program.cs — kept in sync manually since
        // Identity's policy isn't reachable from here without a UserManager instance. This
        // check exists to fail fast with a friendly message; Identity enforces the real rule.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");

        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.TurnstileToken).NotEmpty();
    }
}
