using Microsoft.AspNetCore.Identity;

namespace Identity.Sso.Persistence.Models;

public class ApplicationRole : IdentityRole
{
    public Guid? TenantId { get; set; }
    public string Description { get; set; } = default!;

}
