using System.Security.Claims;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;

public record ProcessAuthorizationQuery(
    ClaimsPrincipal? UserPrincipal,
    string ClientId,
    IReadOnlyCollection<string> Scopes,
    string? Prompt,        // Example: "login" for forcing re-authentication or "consent"
    string? AcrValues,     // Useful if the client sends the tenant in the request (e.g., "tenant:empresa-a")
    string? Display        // To know if a mobile interface or pop-up is requested
) : IRequest<AuthorizationResult>;
