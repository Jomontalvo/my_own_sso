using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Application.Models;
using Identity.Sso.Domain.Enums;
using OpenIddict.Abstractions;

namespace Identity.Sso.Infrastructure.OpenId;

public sealed class OpenIddictClientApplicationStore(IOpenIddictApplicationManager applicationManager)
    : IClientApplicationStore
{
    public async Task<ClientApplication?> FindByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
            return null;

        return new ClientApplication(
            ClientId: await applicationManager.GetClientIdAsync(application, cancellationToken) ?? clientId,
            DisplayName: await applicationManager.GetDisplayNameAsync(application, cancellationToken),
            ConsentType: MapConsentType(await applicationManager.GetConsentTypeAsync(application, cancellationToken)));
    }

    private static ClientConsentType MapConsentType(string? consentType) => consentType switch
    {
        OpenIddictConstants.ConsentTypes.Implicit => ClientConsentType.Implicit,
        OpenIddictConstants.ConsentTypes.External => ClientConsentType.External,
        OpenIddictConstants.ConsentTypes.Systematic => ClientConsentType.Systematic,
        // OpenIddict treats a missing consent type as explicit; so do we.
        _ => ClientConsentType.Explicit
    };
}
