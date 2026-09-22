using Identity.Sso.Domain.Shared;

namespace Identity.Sso.Application.Models;

/// <summary>
/// Outcome of a token request. Carries the revalidated user for user-bound grants,
/// or only the client identifier for the client credentials grant.
/// </summary>
public sealed record TokenDecision
{
    public required bool IsSuccess { get; init; }

    public UserProfile? User { get; init; }

    public string? ClientId { get; init; }

    public IReadOnlyCollection<string> GrantedScopes { get; init; } = [];

    public Error? Error { get; init; }

    public static TokenDecision Success(UserProfile user, IReadOnlyCollection<string> grantedScopes) =>
        new() { IsSuccess = true, User = user, GrantedScopes = grantedScopes };

    public static TokenDecision SuccessForClient(string clientId, IReadOnlyCollection<string> grantedScopes) =>
        new() { IsSuccess = true, ClientId = clientId, GrantedScopes = grantedScopes };

    public static TokenDecision Failure(Error error) =>
        new() { IsSuccess = false, Error = error };
}
