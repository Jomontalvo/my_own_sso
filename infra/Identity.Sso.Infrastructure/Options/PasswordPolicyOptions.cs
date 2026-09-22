using System.ComponentModel.DataAnnotations;

namespace Identity.Sso.Infrastructure.Options;

/// <summary>
/// Password and lockout policy. Defaults are deliberately strict so that a missing
/// configuration section cannot silently disable every requirement.
/// </summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "Identity:PasswordPolicy";

    [Range(8, 128)]
    public int RequiredLength { get; set; } = 12;

    [Range(1, 32)]
    public int RequiredUniqueChars { get; set; } = 4;

    public bool RequireDigit { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; } = true;

    [Range(1, 20)]
    public int MaxFailedAccessAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;
}
