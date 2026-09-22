using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.GetConsentContext;

public record GetConsentContextQuery(
    string ClientId,
    IReadOnlyCollection<string> Scopes) : IRequest<ConsentContext>;
