using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;
using Identity.Sso.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace Identity.Sso.Application.Behaviors.Authorization.Commands.ProcessToken;

public class ProcessTokenUseCase(
    IUserDirectory userDirectory,
    ILogger<ProcessTokenUseCase> logger)
    : IRequestHandler<ProcessTokenCommand, TokenDecision>
{
    public async Task<TokenDecision> Handle(
        ProcessTokenCommand request,
        CancellationToken cancellationToken = default)
    {
        switch (request.GrantKind)
        {
            case TokenGrantKind.AuthorizationCode:
            case TokenGrantKind.RefreshToken:
                // A long-lived refresh token must not outlive the account it was issued for.
                if (request.SubjectId is null)
                    return TokenDecision.Failure(DomainErrors.Token.InvalidGrant);

                var user = await userDirectory.FindBySubjectAsync(request.SubjectId, cancellationToken);
                if (user is null || !user.IsActive)
                {
                    logger.LogWarning(
                        "Token request rejected for subject {SubjectId}: account missing or inactive.",
                        request.SubjectId);
                    return TokenDecision.Failure(DomainErrors.Token.InvalidGrant);
                }

                return TokenDecision.Success(user, request.GrantedScopes);

            case TokenGrantKind.ClientCredentials:
                return string.IsNullOrEmpty(request.ClientId)
                    ? TokenDecision.Failure(DomainErrors.Authorization.InvalidClient)
                    : TokenDecision.SuccessForClient(request.ClientId, request.GrantedScopes);

            default:
                return TokenDecision.Failure(DomainErrors.Token.UnsupportedGrantType);
        }
    }
}
