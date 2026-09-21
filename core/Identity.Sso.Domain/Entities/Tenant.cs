namespace Identity.Sso.Domain.Entities;

public class Tenant
{
    /// <summary>
    /// The unique identifier for the tenant.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The name of the tenant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier string for the tenant. Example: "Secretaría - A".
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The date and time when the tenant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
