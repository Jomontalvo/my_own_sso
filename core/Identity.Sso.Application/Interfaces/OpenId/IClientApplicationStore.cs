using Identity.Sso.Application.Models;

namespace Identity.Sso.Application.Interfaces.OpenId;

public interface IClientApplicationStore
{
    Task<ClientApplication?> FindByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
