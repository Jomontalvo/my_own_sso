using FluentValidation;

namespace Identity.Sso.Application.Behaviors.Authorization.Commands.GrantConsent;

public class GrantConsentCommandValidator : AbstractValidator<GrantConsentCommand>
{
    public GrantConsentCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Scopes).NotEmpty();
    }
}
