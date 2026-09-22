using Identity.Sso.Domain.Shared;

namespace Identity.Sso.Domain.Errors;

public static class DomainErrors
{
    public static class Email
    {
        public static readonly Error Required = new(
            "Email.Required",
            "Email is required.");

        public static readonly Error InvalidFormat = new(
            "Email.InvalidFormat",
            "Email format is invalid.");
    }

    public static class BirthDate
    {
        public static readonly Error MustBeInThePast = new(
            "BirthDate.MustBeInThePast",
            "Birth date must be in the past.");

        public static readonly Error MustBeOfLegalAge = new(
            "BirthDate.MustBeOfLegalAge",
            "Birth date must meet the required legal age.");
    }

    public static class Scope
    {
        public static readonly Error Required = new(
            "Scope.Required",
            "Scope name is required.");

        public static readonly Error InvalidFormat = new(
            "Scope.InvalidFormat",
            "Scope name must follow the 'resource:action' convention using lowercase letters, digits, '_' or '-'.");
    }

    public static class Authorization
    {
        public static readonly Error InvalidClient = new(
            "invalid_client",
            "The specified client is not registered.");

        public static readonly Error LoginRequired = new(
            "login_required",
            "Interactive authentication is required but 'prompt=none' was specified.");

        public static readonly Error ConsentRequired = new(
            "consent_required",
            "User consent is required but 'prompt=none' was specified.");

        public static readonly Error AccountDisabled = new(
            "access_denied",
            "The user account is disabled or no longer exists.");

        public static readonly Error ConsentNotGranted = new(
            "access_denied",
            "The user denied the authorization request.");

        public static readonly Error ExternalConsentMissing = new(
            "consent_required",
            "An administrator must grant access to this client before it can be used.");
    }

    public static class Token
    {
        public static readonly Error InvalidGrant = new(
            "invalid_grant",
            "The token is no longer valid because the account is disabled or no longer exists.");

        public static readonly Error UnsupportedGrantType = new(
            "unsupported_grant_type",
            "The specified grant type is not supported by this authorization server.");
    }
}
