using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;

/// <summary>
/// Decides how an authorization request must continue. Protocol parsing (the raw <c>prompt</c> string,
/// <c>max_age</c>, the session cookie) is resolved by the API before reaching this query.
/// </summary>
public record ProcessAuthorizationQuery(
    string? SubjectId,
    string ClientId,
    IReadOnlyCollection<string> RequestedScopes,
    bool PromptLogin,
    bool PromptConsent,
    bool PromptNone,
    DateTimeOffset? AuthenticatedAt,
    TimeSpan? MaxAge) : IRequest<AuthorizationDecision>;
