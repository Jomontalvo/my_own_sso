using Identity.Sso.Domain.Entities;
using Identity.Sso.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Identity.Sso.Persistence.Configuration;

public class ApplicationRoleConfig : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasIndex(r => r.TenantId).IsUnique(false);
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId);

        // The tenant query filter is applied in ApplicationDbContext, where the current tenant is resolved.

        builder.Property(r => r.Description).HasMaxLength(4000);

    }
}
