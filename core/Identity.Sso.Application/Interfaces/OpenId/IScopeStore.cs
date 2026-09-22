using Identity.Sso.Application.Models;

namespace Identity.Sso.Application.Interfaces.OpenId;

public interface IScopeStore
{
    Task<IReadOnlyList<ScopeDescription>> DescribeAsync(
        IReadOnlyCollection<string> scopeNames,
        CancellationToken cancellationToken = default);

    /// <summary>Resource servers (audiences) associated with the given scopes.</summary>
    Task<IReadOnlyList<string>> ListResourcesAsync(
        IReadOnlyCollection<string> scopeNames,
        CancellationToken cancellationToken = default);
}
