using Identity.Sso.Domain.Enums;
using Identity.Sso.Domain.Shared;

namespace Identity.Sso.Application.Models;

/// <summary>
/// Outcome of an authorization request, expressed without any protocol or transport type.
/// The API layer translates it into an OpenIddict sign-in, challenge, redirect or forbid.
/// </summary>
public sealed record AuthorizationDecision
{
    public required AuthorizationOutcome Outcome { get; init; }

    public UserProfile? User { get; init; }

    public ClientApplication? Client { get; init; }

    public IReadOnlyCollection<string> GrantedScopes { get; init; } = [];

    public string? AuthorizationId { get; init; }

    public Error? Error { get; init; }

    public static AuthorizationDecision Challenge() =>
        new() { Outcome = AuthorizationOutcome.Challenge };

    public static AuthorizationDecision Consent(
        UserProfile user,
        ClientApplication client,
        IReadOnlyCollection<string> requestedScopes) =>
        new()
        {
            Outcome = AuthorizationOutcome.Consent,
            User = user,
            Client = client,
            GrantedScopes = requestedScopes
        };

    public static AuthorizationDecision Grant(
        UserProfile user,
        ClientApplication client,
        IReadOnlyCollection<string> grantedScopes,
        string? authorizationId) =>
        new()
        {
            Outcome = AuthorizationOutcome.Grant,
            User = user,
            Client = client,
            GrantedScopes = grantedScopes,
            AuthorizationId = authorizationId
        };

    public static AuthorizationDecision Deny(Error error) =>
        new() { Outcome = AuthorizationOutcome.Deny, Error = error };
}
