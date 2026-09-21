using Identity.Sso.Domain.Entities;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Identity.Sso.Persistence.Configuration;

public class ApplicationRoleConfig(ApplicationDbContext dbContext) : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasIndex(r => r.TenantId).IsUnique(false);
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId);

        builder.HasQueryFilter(r => r.TenantId == dbContext.CurrentTenantId || r.TenantId == null || dbContext.CurrentTenantId == null);

        builder.Property(r => r.Description).HasMaxLength(4000);

    }
}
