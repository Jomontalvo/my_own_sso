using FluentValidation;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;

public class ProcessAuthorizationQueryValidator : AbstractValidator<ProcessAuthorizationQuery>
{
    public ProcessAuthorizationQueryValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("The 'client_id' parameter is required.");

        RuleFor(x => x.RequestedScopes)
            .NotNull().WithMessage("The 'scope' parameter is required.");

        RuleFor(x => x.MaxAge)
            .Must(maxAge => maxAge is null || maxAge >= TimeSpan.Zero)
            .WithMessage("The 'max_age' parameter must not be negative.");
    }
}
