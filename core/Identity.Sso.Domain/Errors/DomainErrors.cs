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
}