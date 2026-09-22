using System.Security.Claims;
using Identity.Sso.Application.Models;

namespace Identity.Sso.Application.Interfaces.Persistence;

public interface IIdentityRepository
{
    Task<AuthorizationResult> ProcessAuthorizationAsync(
        ClaimsPrincipal? userPrincipal,
        string clientId,
        IReadOnlyCollection<string> scopes,
        string? prompt,
        string? acrValues,
        string? display,
        CancellationToken cancellationToken = default);
}
