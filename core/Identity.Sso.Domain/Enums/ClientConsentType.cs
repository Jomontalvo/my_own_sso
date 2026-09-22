namespace Identity.Sso.Domain.Enums;

public enum ClientConsentType
{
    /// <summary>Consent is never asked for: the client is fully trusted.</summary>
    Implicit,

    /// <summary>Consent is asked for the first time and remembered afterwards.</summary>
    Explicit,

    /// <summary>Consent must be granted out of band by an administrator.</summary>
    External,

    /// <summary>Consent is asked on every authorization request.</summary>
    Systematic
}
