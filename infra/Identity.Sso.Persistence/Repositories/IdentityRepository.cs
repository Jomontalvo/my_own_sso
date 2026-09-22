using System.Security.Claims;
using Identity.Sso.Application.Interfaces.Persistence;
using Identity.Sso.Application.Models;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
namespace Identity.Sso.Persistence.Repositories;

public class IdentityRepository(
    UserManager<ApplicationUser> userManager,
    ILogger<IdentityRepository> logger) : IIdentityRepository
{
    public async Task<AuthorizationResult> ProcessAuthorizationAsync(ClaimsPrincipal? userPrincipal,
        string clientId,
        IReadOnlyCollection<string> scopes,
        string? prompt,
        string? acrValues,
        string? display, CancellationToken cancellationToken = default)
    {
        // 1. Validate basic authentication
        if (userPrincipal?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("User is not authenticated. Returning challenge.");
            return AuthorizationResult.RequireChallenge();
        }

        // 2. Get user from the database
        var user = await userManager.GetUserAsync(userPrincipal);
        if (user == null || !user.IsActive)
        {
            logger.LogWarning("User not found or inactive. Returning forbid.");
            return AuthorizationResult.Forbid();
        }

        // 3. Create user Principal Claims
        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("email", user.Email ?? string.Empty),
            new("name", $"{user.FirstName} {user.LastName}".Trim()),
            new("tenant_id", user.TenantId.ToString())
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "OpenIddict.Server.AspNetCore");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // 4. Assign the extracted scopes to the claims principal
        claimsPrincipal.SetScopes(scopes);

        // 5. Return the authorization result
        return AuthorizationResult.Success(claimsPrincipal);
    }
}
