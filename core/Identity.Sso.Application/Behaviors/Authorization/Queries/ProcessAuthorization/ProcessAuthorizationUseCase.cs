using Identity.Sso.Application.Interfaces.Persistence;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;

public class ProcessAuthorizationUseCase(IIdentityRepository repository) : IRequestHandler<ProcessAuthorizationQuery, AuthorizationResult>
{
    public async Task<AuthorizationResult> Handle(ProcessAuthorizationQuery request, CancellationToken cancellationToken)
    {
        var userPrincipal = request.UserPrincipal;
        var clientId = request.ClientId;
        var scopes = request.Scopes;
        var prompt = request.Prompt;
        var acrValues = request.AcrValues;
        var display = request.Display;

        var result = await repository.ProcessAuthorizationAsync(userPrincipal, clientId, scopes, prompt, acrValues, display, cancellationToken);
        return result;
    }
}
