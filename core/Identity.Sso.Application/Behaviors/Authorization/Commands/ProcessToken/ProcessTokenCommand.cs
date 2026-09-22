using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;

namespace Identity.Sso.Application.Behaviors.Authorization.Commands.ProcessToken;

/// <summary>
/// Revalidates a token request. The authorization code and refresh token themselves are verified by
/// OpenIddict before this command runs; what remains is confirming the subject is still allowed in.
/// </summary>
public record ProcessTokenCommand(
    TokenGrantKind GrantKind,
    string? SubjectId,
    string? ClientId,
    IReadOnlyCollection<string> GrantedScopes) : IRequest<TokenDecision>;

