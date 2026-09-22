namespace Identity.Sso.Domain.Enums;

public enum CredentialValidationOutcome
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
    NotAllowed,
    RequiresTwoFactor,
    Inactive
}
