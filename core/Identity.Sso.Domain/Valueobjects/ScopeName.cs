using System.Text.RegularExpressions;
using Identity.Sso.Domain.Errors;
using Identity.Sso.Domain.Shared;

namespace Identity.Sso.Domain.ValueObjects;

/// <summary>
/// An OAuth 2.0 scope token. Enforces the repository convention <c>resource:action</c> on top of RFC 6749 §3.3.
/// </summary>
public sealed partial class ScopeName : IEquatable<ScopeName>
{
    private static readonly Regex ScopeRegex = RegexScope();

    public string Value { get; }

    private ScopeName(string value) => Value = value;

    public static Result<ScopeName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<ScopeName>(DomainErrors.Scope.Required);

        var normalized = value.Trim();

        if (!ScopeRegex.IsMatch(normalized))
            return Result.Failure<ScopeName>(DomainErrors.Scope.InvalidFormat);

        return Result.Success(new ScopeName(normalized));
    }

    public override string ToString() => Value;

    public bool Equals(ScopeName? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as ScopeName);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static implicit operator string(ScopeName scope) => scope.Value;

    // RFC 6749 scope-token charset, restricted to the "resource:action" shape used across SIGOB.
    [GeneratedRegex(@"^[a-z][a-z0-9_-]*(:[a-z][a-z0-9_-]*)?$", RegexOptions.Compiled)]
    private static partial Regex RegexScope();
}
