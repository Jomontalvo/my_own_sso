using Identity.Sso.Domain.Entities;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Sso.Persistence.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    // Optional interface to resolve the current Tenant during executions
    public Guid? CurrentTenantId { get; set; }

    // Tables
    public DbSet<Tenant> Tenants { get; set; } = default!;

    /// <summary>
    /// Configures the model for the context, applying configurations and disabling cascading delete.
    /// </summary>
    /// <param name="builder"></param>
    /// <remarks>
    /// This method applies all entity configurations from the assembly and disables cascading delete for all relationships.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Rename default Identity tables to custom names
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // 2. Register OpenIddict entities in EF Core model
        builder.UseOpenIddict();

        // ApplicationUserConfig/ApplicationRoleConfig need the current DbContext instance (for CurrentTenantId), so they're excluded from the assembly scan and applied manually.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly,
            type => type == typeof(Configuration.ApplicationUserConfig) || type == typeof(Configuration.ApplicationRoleConfig));
        builder.ApplyConfiguration(new Configuration.ApplicationUserConfig(this));
        builder.ApplyConfiguration(new Configuration.ApplicationRoleConfig(this));

        DisableCascadingDelete(builder);
    }

    /// <summary>
    /// Disables cascading delete for all relationships in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity relationships.</param>
    private static void DisableCascadingDelete(ModelBuilder modelBuilder)
    {
        var relationships = modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys());
        foreach (var relationship in relationships)
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

}
