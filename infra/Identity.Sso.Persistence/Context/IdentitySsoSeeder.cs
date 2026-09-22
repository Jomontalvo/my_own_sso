using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Identity.Sso.Persistence.Models;
using OpenIddict.Abstractions;
using Identity.Sso.Domain.Entities;

namespace Identity.Sso.Persistence.Context;

public sealed class IdentitySsoSeeder(
    IServiceProvider serviceProvider,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<IdentitySsoSeeder> logger) : IHostedService
{
    /// <summary>
    /// Seeds the database with initial data if necessary.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("IdentitySsoDatabaseSeeder: Starting database verification...");

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Identity Seeder: Database updated successfully.");

        await SeedAsync(serviceProvider, context, cancellationToken);
    }

    /// <summary>
    /// Stops the database seeder.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;


    private static async Task SeedAsync(IServiceProvider serviceProvider, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        // 1. Create demo Tenant
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant = await context.Tenants.FindAsync([tenantId], cancellationToken: cancellationToken);
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Id = tenantId,
                Name = "Default Organization",
                Identifier = "sigob-default-organization",
                IsActive = true
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Create Role and Demo User
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin",
                Description = "System Administrator",
                TenantId = tenant.Id
            });
        }
        var adminEmail = "admin@sigob.org";
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Demo",
                TenantId = tenant.Id,
                IsActive = true
            };
            await userManager.CreateAsync(user, "Password@123!");
            await userManager.AddToRoleAsync(user, "Admin");
        }

        // 3. Regiter Custom Scopes on OpenIddict
        if (await scopeManager.FindByNameAsync("interop-admin", cancellationToken) == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "interop-admin",
                DisplayName = "Interop Administrator Access",
                Resources = { "resource_server" }
            }, cancellationToken);
        }

        // 4. Register OIDC Client (Blazor / SPA - Public Client)
        if (await appManager.FindByClientIdAsync("blazor-spa-client", cancellationToken) == null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "blazor-spa-client",
                DisplayName = "Blazor Single Sign On WebAssembly Client",
                ApplicationType = OpenIddictConstants.ClientTypes.Public,
                RedirectUris = { new Uri("https://localhost:7026/signin-oidc") },
                PostLogoutRedirectUris = { new Uri("https://localhost:7026/signout-callback-oidc") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "sso-admin"
                },
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            }, cancellationToken);
        }
    }
}