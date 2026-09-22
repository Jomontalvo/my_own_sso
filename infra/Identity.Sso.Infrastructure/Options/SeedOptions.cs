using System.ComponentModel.DataAnnotations;

namespace Identity.Sso.Infrastructure.Options;

/// <summary>Seed data applied at startup. The admin account is only created outside production.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public Guid TenantId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Required]
    public string TenantName { get; set; } = "Default Organization";

    [Required]
    public string TenantIdentifier { get; set; } = "sigob-default-organization";

    [EmailAddress]
    public string AdministratorEmail { get; set; } = "admin@sigob.org";

    /// <summary>When empty the administrator account is not seeded.</summary>
    public string? AdministratorPassword { get; set; }

    public SeedClientOptions[] Clients { get; set; } = [];
}

public sealed class SeedClientOptions
{
    [Required]
    public string ClientId { get; set; } = default!;

    public string? ClientSecret { get; set; }

    public string? DisplayName { get; set; }

    public string[] RedirectUris { get; set; } = [];

    public string[] PostLogoutRedirectUris { get; set; } = [];

    public string[] Scopes { get; set; } = [];

    /// <summary>When true the client uses the client credentials grant instead of the interactive flow.</summary>
    public bool MachineToMachine { get; set; }

    /// <summary>Restricts the client to non-production environments.</summary>
    public bool DevelopmentOnly { get; set; }
}
