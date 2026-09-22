namespace Identity.Sso.Domain.Enums;

public enum AuthorizationOutcome
{
    /// <summary>No active session: the user must authenticate before the request can continue.</summary>
    Challenge,

    /// <summary>The user must explicitly approve the requested scopes for this client.</summary>
    Consent,

    /// <summary>The authorization can be issued.</summary>
    Grant,

    /// <summary>The request must be rejected with an OAuth error.</summary>
    Deny
}
