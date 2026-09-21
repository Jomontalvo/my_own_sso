using Identity.Sso.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;


namespace Identity.Sso.Persistence.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public Guid TenantId { get; set; }
    public BirthDate BirthDate { get; set; }
    public bool BiometricIdentityConfirmed { get; set; }
    public string? ImageFileUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
