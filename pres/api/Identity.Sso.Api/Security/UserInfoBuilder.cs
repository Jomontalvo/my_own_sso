using Identity.Sso.Application.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Sso.Api.Security;

/// <summary>
/// Maps a user profile to the <c>/connect/userinfo</c> response, filtered by the scopes actually granted
/// (OpenID Connect Core 1.0, §5.4).
/// </summary>
internal static class UserInfoBuilder
{
    public static Dictionary<string, object> Build(UserProfile user, IReadOnlyCollection<string> grantedScopes)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.SubjectId
        };

        if (grantedScopes.Contains(Scopes.Profile))
        {
            claims[Claims.Name] = user.FullName;
            claims[Claims.GivenName] = user.FirstName;
            claims[Claims.FamilyName] = user.LastName;
            claims[Claims.PreferredUsername] = user.UserName;
            claims[Claims.Birthdate] = user.BirthDate.ToString("yyyy-MM-dd");

            if (!string.IsNullOrWhiteSpace(user.ImageFileUrl))
                claims[Claims.Picture] = user.ImageFileUrl;
        }

        if (grantedScopes.Contains(Scopes.Email) && !string.IsNullOrWhiteSpace(user.Email))
        {
            claims[Claims.Email] = user.Email;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        if (grantedScopes.Contains(Scopes.Roles))
            claims[Claims.Role] = user.Roles;

        claims[OpenIddictPrincipalFactory.TenantIdClaim] = user.TenantId;

        return claims;
    }
}
