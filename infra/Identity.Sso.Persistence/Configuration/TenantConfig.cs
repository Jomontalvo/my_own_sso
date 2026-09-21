using Identity.Sso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Sso.Persistence.Configuration;

public class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(250);
        builder.HasIndex(t => t.Identifier).IsUnique();
        builder.Property(t => t.Identifier)    
            .IsRequired()
            .HasMaxLength(250);
    }
}
