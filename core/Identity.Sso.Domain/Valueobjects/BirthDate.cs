using Identity.Sso.Domain.Errors;
using Identity.Sso.Domain.Shared;

namespace Identity.Sso.Domain.ValueObjects;

public readonly record struct BirthDate
{

    public DateOnly Value { get; }

    private BirthDate(DateOnly value)
    {
        Value = value;
    }

    public static Result<BirthDate> Create(DateOnly value)
    {
        if (value >= DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure<BirthDate>(DomainErrors.BirthDate.MustBeInThePast);

        if (!IsLegalAge(value, 18))
            return Result.Failure<BirthDate>(DomainErrors.BirthDate.MustBeOfLegalAge);

        return Result.Success(new BirthDate(value));
    }

    /// <summary>
    /// Check if the birth date indicates legal age
    /// </summary>
    /// <param name="birthDate"></param>
    /// <param name="requiredAge"></param>
    /// <returns></returns>
    private static bool IsLegalAge(DateOnly birthDate, int requiredAge)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        int age = today.Year - birthDate.Year;

        // Adjust if the birthday has not occurred yet this year
        if (birthDate > today.AddYears(-age))
            age--;

        return age >= requiredAge;
    }

    public override string ToString() => Value.ToString("yyyy-MM-dd");

    // Implicit conversion: allows treating BirthDate as DateOnly
    public static implicit operator DateOnly(BirthDate birthDate) => birthDate.Value;

    // Reconstructs from already-validated persisted data, bypassing domain validation - for EF Core value conversion only
    internal static BirthDate FromPersistence(DateOnly value) => new(value);
}
