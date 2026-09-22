using Identity.Sso.Application.Exceptions;
using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.GetConsentContext;

public class GetConsentContextUseCase(
    IClientApplicationStore clientStore,
    IScopeStore scopeStore)
    : IRequestHandler<GetConsentContextQuery, ConsentContext>
{
    public async Task<ConsentContext> Handle(GetConsentContextQuery request, CancellationToken cancellationToken = default)
    {
        var client = await clientStore.FindByClientIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException($"Client '{request.ClientId}' is not registered.");

        var scopes = await scopeStore.DescribeAsync(request.Scopes, cancellationToken);

        return new ConsentContext(client, scopes);
    }
}
