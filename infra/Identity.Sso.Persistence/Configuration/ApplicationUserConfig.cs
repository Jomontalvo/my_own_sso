using Identity.Sso.Domain.Entities;
using Identity.Sso.Domain.ValueObjects;
using Identity.Sso.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Sso.Persistence.Configuration;

public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.HasIndex(u => u.TenantId).IsUnique(false);

        // 1. Foreign Key Relationship with Tenant
        // DeleteBehavior is forced to Restrict for every FK by DisableCascadingDelete in OnModelCreating, no need to set it here.
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId);

        // The tenant query filter is applied in ApplicationDbContext, where the current tenant is resolved.

        builder.Property(u => u.FirstName).HasMaxLength(50);
        builder.Property(u => u.LastName).HasMaxLength(50);
        builder.Property(u => u.ImageFileUrl).IsUnicode();

        builder.Property(u => u.BirthDate)
            .HasConversion(bd => bd.Value, v => BirthDate.FromPersistence(v))
            .HasColumnName("BirthDate")
            .IsRequired();
    }
}
