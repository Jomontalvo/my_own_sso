using Identity.Sso.Application.Exceptions;
using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Commands.GrantConsent;

public class GrantConsentUseCase(
    IUserDirectory userDirectory,
    IClientApplicationStore clientStore,
    IAuthorizationStore authorizationStore)
    : IRequestHandler<GrantConsentCommand, string>
{
    public async Task<string> Handle(GrantConsentCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userDirectory.FindBySubjectAsync(request.SubjectId, cancellationToken)
            ?? throw new NotFoundException($"User '{request.SubjectId}' was not found.");

        if (!user.IsActive)
            throw new NotFoundException($"User '{request.SubjectId}' is not active.");

        var client = await clientStore.FindByClientIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException($"Client '{request.ClientId}' is not registered.");

        var existing = await authorizationStore.FindValidAuthorizationIdAsync(
            user.SubjectId, client.ClientId, request.Scopes, cancellationToken);

        return existing ?? await authorizationStore.CreatePermanentAuthorizationAsync(
            user.SubjectId, client.ClientId, request.Scopes, cancellationToken);
    }
}
