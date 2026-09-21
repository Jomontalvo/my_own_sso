using System.Text.RegularExpressions;
using Identity.Sso.Domain.Errors;
using Identity.Sso.Domain.Shared;

namespace Identity.Sso.Domain.ValueObjects;

public sealed partial class Email
{
    private static readonly Regex EmailRegex = RegexEmail();

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>(DomainErrors.Email.Required);

        string normalizedValue = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalizedValue))
            return Result.Failure<Email>(DomainErrors.Email.InvalidFormat);

        return Result.Success(new Email(normalizedValue));
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex RegexEmail();
}