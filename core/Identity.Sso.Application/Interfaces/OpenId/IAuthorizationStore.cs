namespace Identity.Sso.Application.Interfaces.OpenId;

/// <summary>
/// Persistence of user consent. An authorization records that a subject approved a set of scopes for a client.
/// </summary>
public interface IAuthorizationStore
{
    /// <summary>Returns the id of a valid, permanent authorization covering <paramref name="scopes"/>, or null.</summary>
    Task<string?> FindValidAuthorizationIdAsync(
        string subjectId,
        string clientId,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken = default);

    Task<string> CreatePermanentAuthorizationAsync(
        string subjectId,
        string clientId,
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken = default);
}
