using System.Collections.Immutable;
using Identity.Sso.Application.Interfaces.OpenId;
using OpenIddict.Abstractions;

namespace Identity.Sso.Infrastructure.OpenId;

public sealed class OpenIddictAuthorizationStore(
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager) : IAuthorizationStore
{
    public async Task<string?> FindValidAuthorizationIdAsync(
        string subjectId,
        string clientId,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken = default)
    {
        var applicationId = await ResolveApplicationIdAsync(clientId, cancellationToken);
        if (applicationId is null)
            return null;

        var authorizations = authorizationManager.FindAsync(
            subject: subjectId,
            client: applicationId,
            status: OpenIddictConstants.Statuses.Valid,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: [.. scopes],
            cancellationToken: cancellationToken);

        await foreach (var authorization in authorizations)
        {
            return await authorizationManager.GetIdAsync(authorization, cancellationToken);
        }

        return null;
    }

    public async Task<string> CreatePermanentAuthorizationAsync(
        string subjectId,
        string clientId,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken = default)
    {
        var applicationId = await ResolveApplicationIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException($"Client '{clientId}' is not registered.");

        var descriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = applicationId,
            Subject = subjectId,
            Type = OpenIddictConstants.AuthorizationTypes.Permanent,
            Status = OpenIddictConstants.Statuses.Valid,
            CreationDate = DateTimeOffset.UtcNow
        };

        descriptor.Scopes.UnionWith(scopes);

        var authorization = await authorizationManager.CreateAsync(descriptor, cancellationToken);

        return await authorizationManager.GetIdAsync(authorization, cancellationToken)
            ?? throw new InvalidOperationException("The created authorization has no identifier.");
    }

    private async Task<string?> ResolveApplicationIdAsync(string clientId, CancellationToken cancellationToken)
    {
        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        return application is null ? null : await applicationManager.GetIdAsync(application, cancellationToken);
    }
}
