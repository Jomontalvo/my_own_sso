using Identity.Sso.Domain.Entities;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Sso.Persistence.Configuration;

public class ApplicationUserConfig(ApplicationDbContext dbContext) : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.HasIndex(u => u.TenantId).IsUnique(false);
        builder.Property(u => u.TenantId).HasMaxLength(450);

        // 1. Foreign Key Relationship with Tenant
        // DeleteBehavior is forced to Restrict for every FK by DisableCascadingDelete in OnModelCreating, no need to set it here.
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId);

        // 2. Isolated Logical (Global Filter) - resolved from the DbContext instance, since Configure() has no context access on its own
        builder.HasQueryFilter(u => u.TenantId == dbContext.CurrentTenantId || dbContext.CurrentTenantId == null);

        builder.Property(u => u.FirstName).HasMaxLength(50);
        builder.Property(u => u.LastName).HasMaxLength(50);
        builder.Property(u => u.ImageFileUrl).IsUnicode();
    }
}
