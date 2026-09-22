using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Application.Models;
using OpenIddict.Abstractions;

namespace Identity.Sso.Infrastructure.OpenId;

public sealed class OpenIddictScopeStore(IOpenIddictScopeManager scopeManager) : IScopeStore
{
    public async Task<IReadOnlyList<ScopeDescription>> DescribeAsync(
        IReadOnlyCollection<string> scopeNames,
        CancellationToken cancellationToken = default)
    {
        var descriptions = new List<ScopeDescription>(scopeNames.Count);

        foreach (var name in scopeNames)
        {
            var scope = await scopeManager.FindByNameAsync(name, cancellationToken);

            // Standard OIDC scopes (openid, profile, ...) are not persisted; fall back to the raw name.
            descriptions.Add(scope is null
                ? new ScopeDescription(name, null, null)
                : new ScopeDescription(
                    name,
                    await scopeManager.GetDisplayNameAsync(scope, cancellationToken),
                    await scopeManager.GetDescriptionAsync(scope, cancellationToken)));
        }

        return descriptions;
    }

    public async Task<IReadOnlyList<string>> ListResourcesAsync(
        IReadOnlyCollection<string> scopeNames,
        CancellationToken cancellationToken = default)
    {
        var resources = new List<string>();

        await foreach (var resource in scopeManager.ListResourcesAsync([.. scopeNames], cancellationToken))
        {
            resources.Add(resource);
        }

        return resources;
    }
}
