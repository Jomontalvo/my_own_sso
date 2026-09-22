using Identity.Sso.Domain.Entities;
using Identity.Sso.Infrastructure.Options;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Identity.Sso.Infrastructure.Seeding;

/// <summary>
/// Applies pending migrations and seeds the default tenant, roles, scopes and clients.
/// Credentials come from configuration; the administrator account is never seeded in production.
/// </summary>
public sealed class IdentitySsoSeeder(
    IServiceProvider serviceProvider,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IOptions<SeedOptions> seedOptions,
    IOptions<OidcOptions> oidcOptions,
    IHostEnvironment environment,
    ILogger<IdentitySsoSeeder> logger) : IHostedService
{
    private readonly SeedOptions _seed = seedOptions.Value;
    private readonly OidcOptions _oidc = oidcOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Identity SSO seeder: applying migrations...");

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);

        using var scope = serviceProvider.CreateScope();

        var tenant = await SeedTenantAsync(context, cancellationToken);
        await SeedAdministratorAsync(scope.ServiceProvider, tenant, cancellationToken);
        await SeedScopesAsync(scope.ServiceProvider, cancellationToken);
        await SeedClientsAsync(scope.ServiceProvider, cancellationToken);

        logger.LogInformation("Identity SSO seeder: completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<Tenant> SeedTenantAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.FindAsync([_seed.TenantId], cancellationToken);
        if (tenant is not null)
            return tenant;

        tenant = new Tenant
        {
            Id = _seed.TenantId,
            Name = _seed.TenantName,
            Identifier = _seed.TenantIdentifier,
            IsActive = true
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    private async Task SeedAdministratorAsync(
        IServiceProvider scopedProvider,
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        var roleManager = scopedProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin",
                Description = "System Administrator",
                TenantId = tenant.Id
            });
        }

        if (environment.IsProduction())
        {
            logger.LogInformation("Identity SSO seeder: administrator seeding skipped in production.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_seed.AdministratorPassword))
        {
            logger.LogWarning(
                "Identity SSO seeder: no administrator password configured ('{Section}:AdministratorPassword'); account not seeded.",
                SeedOptions.SectionName);
            return;
        }

        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(_seed.AdministratorEmail) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = _seed.AdministratorEmail,
            Email = _seed.AdministratorEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "SIGOB",
            TenantId = tenant.Id,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, _seed.AdministratorPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Identity SSO seeder: could not create the administrator account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, "Admin");
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task SeedScopesAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var scopeManager = scopedProvider.GetRequiredService<IOpenIddictScopeManager>();

        foreach (var scopeName in _oidc.Scopes)
        {
            if (await scopeManager.FindByNameAsync(scopeName, cancellationToken) is not null)
                continue;

            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = scopeName,
                DisplayName = scopeName,
                Resources = { "sigob-resource-server" }
            }, cancellationToken);
        }
    }

    private async Task SeedClientsAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var applicationManager = scopedProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in _seed.Clients)
        {
            if (client.DevelopmentOnly && environment.IsProduction())
                continue;

            if (await applicationManager.FindByClientIdAsync(client.ClientId, cancellationToken) is not null)
                continue;

            // Secrets never live in appsettings: they come from user-secrets, environment variables or a vault.
            if (client.MachineToMachine && string.IsNullOrWhiteSpace(client.ClientSecret))
            {
                logger.LogWarning(
                    "Identity SSO seeder: confidential client {ClientId} skipped because no secret is configured ('{Section}:Clients').",
                    client.ClientId, SeedOptions.SectionName);
                continue;
            }

            await applicationManager.CreateAsync(BuildDescriptor(client), cancellationToken);
            logger.LogInformation("Identity SSO seeder: registered client {ClientId}.", client.ClientId);
        }
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(SeedClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName ?? client.ClientId,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit
        };

        if (client.MachineToMachine)
        {
            descriptor.ClientSecret = client.ClientSecret;
            descriptor.ClientType = OpenIddictConstants.ClientTypes.Confidential;
            descriptor.ConsentType = OpenIddictConstants.ConsentTypes.Implicit;
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        }
        else
        {
            descriptor.ClientType = string.IsNullOrEmpty(client.ClientSecret)
                ? OpenIddictConstants.ClientTypes.Public
                : OpenIddictConstants.ClientTypes.Confidential;
            descriptor.ClientSecret = client.ClientSecret;

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

            foreach (var uri in client.RedirectUris)
                descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));

            foreach (var uri in client.PostLogoutRedirectUris)
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var scope in client.Scopes)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);

        return descriptor;
    }
}
