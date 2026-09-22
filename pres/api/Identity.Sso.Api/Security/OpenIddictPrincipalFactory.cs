using System.Security.Claims;
using Identity.Sso.Application.Models;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Sso.Api.Security;

/// <summary>
/// Builds the OpenIddict principal from an Application <see cref="UserProfile"/>.
/// Claim construction and token destinations are protocol concerns and live here, not in the inner layers.
/// </summary>
internal static class OpenIddictPrincipalFactory
{
    internal const string TenantIdClaim = "tenant_id";

    public static ClaimsPrincipal ForUser(
        UserProfile user,
        IEnumerable<string> grantedScopes,
        IEnumerable<string> resources,
        string? authorizationId)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, user.SubjectId));
        identity.AddClaim(new Claim(Claims.Name, user.FullName));
        identity.AddClaim(new Claim(Claims.PreferredUsername, user.UserName));
        identity.AddClaim(new Claim(Claims.GivenName, user.FirstName));
        identity.AddClaim(new Claim(Claims.FamilyName, user.LastName));
        identity.AddClaim(new Claim(Claims.Birthdate, user.BirthDate.ToString("yyyy-MM-dd")));
        identity.AddClaim(new Claim(TenantIdClaim, user.TenantId.ToString()));

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.AddClaim(new Claim(Claims.Email, user.Email));
            // SetClaim keeps the JSON boolean type required by OIDC for email_verified.
            identity.SetClaim(Claims.EmailVerified, user.EmailConfirmed);
        }

        if (!string.IsNullOrWhiteSpace(user.ImageFileUrl))
            identity.AddClaim(new Claim(Claims.Picture, user.ImageFileUrl));

        foreach (var role in user.Roles)
            identity.AddClaim(new Claim(Claims.Role, role));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(grantedScopes);
        principal.SetResources(resources);

        if (!string.IsNullOrWhiteSpace(authorizationId))
            principal.SetAuthorizationId(authorizationId);

        principal.SetDestinations(claim => ResolveDestinations(claim, principal));

        return principal;
    }

    public static ClaimsPrincipal ForClient(
        string clientId,
        IEnumerable<string> grantedScopes,
        IEnumerable<string> resources)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, clientId));
        identity.AddClaim(new Claim(Claims.Name, clientId));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(grantedScopes);
        principal.SetResources(resources);
        principal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Subject or Claims.Name => [Destinations.AccessToken],
            _ => []
        });

        return principal;
    }

    /// <summary>
    /// Keeps personally identifiable information out of the access token: PII is only exposed through the
    /// ID token and <c>/connect/userinfo</c>. This is what makes switching to an encrypted access token later
    /// a non-breaking change for clients.
    /// </summary>
    private static IEnumerable<string> ResolveDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case Claims.Subject:
            case TenantIdClaim:
                return [Destinations.AccessToken, Destinations.IdentityToken];

            case Claims.Role:
                return principal.HasScope(Scopes.Roles)
                    ? [Destinations.AccessToken, Destinations.IdentityToken]
                    : [];

            case Claims.Name:
            case Claims.PreferredUsername:
            case Claims.GivenName:
            case Claims.FamilyName:
            case Claims.Birthdate:
            case Claims.Picture:
                return principal.HasScope(Scopes.Profile) ? [Destinations.IdentityToken] : [];

            case Claims.Email:
            case Claims.EmailVerified:
                return principal.HasScope(Scopes.Email) ? [Destinations.IdentityToken] : [];

            // Never leaves the authorization server.
            case "AspNet.Identity.SecurityStamp":
            default:
                return [];
        }
    }
}
