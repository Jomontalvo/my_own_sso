using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;
using Identity.Sso.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;

public class ProcessAuthorizationUseCase(
    IUserDirectory userDirectory,
    IClientApplicationStore clientStore,
    IAuthorizationStore authorizationStore,
    TimeProvider timeProvider,
    ILogger<ProcessAuthorizationUseCase> logger)
    : IRequestHandler<ProcessAuthorizationQuery, AuthorizationDecision>
{
    public async Task<AuthorizationDecision> Handle(
        ProcessAuthorizationQuery request,
        CancellationToken cancellationToken = default)
    {
        // 1. The client must be registered before anything else is evaluated.
        var client = await clientStore.FindByClientIdAsync(request.ClientId, cancellationToken);
        if (client is null)
        {
            logger.LogWarning("Authorization requested by unknown client {ClientId}.", request.ClientId);
            return AuthorizationDecision.Deny(DomainErrors.Authorization.InvalidClient);
        }

        // 2. Interactive authentication: no session, forced re-authentication, or an expired max_age.
        if (request.SubjectId is null || request.PromptLogin || IsSessionTooOld(request))
        {
            return request.PromptNone
                ? AuthorizationDecision.Deny(DomainErrors.Authorization.LoginRequired)
                : AuthorizationDecision.Challenge();
        }

        // 3. The session may outlive the account: revalidate on every authorization request.
        var user = await userDirectory.FindBySubjectAsync(request.SubjectId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Authorization denied for subject {SubjectId}: account missing or inactive.", request.SubjectId);
            return AuthorizationDecision.Deny(DomainErrors.Authorization.AccountDisabled);
        }

        var authorizationId = await authorizationStore.FindValidAuthorizationIdAsync(
            user.SubjectId, client.ClientId, request.RequestedScopes, cancellationToken);

        // 4. Consent, driven by the client registration.
        return client.ConsentType switch
        {
            ClientConsentType.Implicit =>
                AuthorizationDecision.Grant(user, client, request.RequestedScopes, authorizationId),

            ClientConsentType.External when authorizationId is null =>
                AuthorizationDecision.Deny(DomainErrors.Authorization.ExternalConsentMissing),

            ClientConsentType.External =>
                AuthorizationDecision.Grant(user, client, request.RequestedScopes, authorizationId),

            ClientConsentType.Systematic =>
                RequireConsent(request, user, client),

            // Explicit: remembered once granted, unless the client asks for consent again.
            _ when authorizationId is not null && !request.PromptConsent =>
                AuthorizationDecision.Grant(user, client, request.RequestedScopes, authorizationId),

            _ => RequireConsent(request, user, client)
        };
    }

    private static AuthorizationDecision RequireConsent(
        ProcessAuthorizationQuery request,
        UserProfile user,
        ClientApplication client) =>
        request.PromptNone
            ? AuthorizationDecision.Deny(DomainErrors.Authorization.ConsentRequired)
            : AuthorizationDecision.Consent(user, client, request.RequestedScopes);

    private bool IsSessionTooOld(ProcessAuthorizationQuery request)
    {
        if (request.MaxAge is not { } maxAge)
            return false;

        return request.AuthenticatedAt is not { } authenticatedAt
            || timeProvider.GetUtcNow() - authenticatedAt > maxAge;
    }
}
