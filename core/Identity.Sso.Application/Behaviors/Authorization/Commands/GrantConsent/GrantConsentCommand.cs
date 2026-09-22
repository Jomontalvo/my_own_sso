using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Commands.GrantConsent;

public record GrantConsentCommand(
    string SubjectId,
    string ClientId,
    IReadOnlyCollection<string> Scopes) : IRequest<string>;
