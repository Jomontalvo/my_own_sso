using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Sso.Persistence;

public static class DependencyContainer
{
    /// <summary>
    /// Adds the persistence services to the service collection, including DbContext, repositories, and unit of work.
    /// </summary>
    /// <param name="services">The service collection to which the persistence services will be added.</param>
    /// <returns>The updated service collection with the persistence services added.</returns>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register ApplicationDbContex
        var connectionString = configuration.GetConnectionString("IdentityConnection")
            ?? throw new InvalidOperationException("Connection string 'IdentityConnection' not found.");

        // 2. Factory (Singleton) for the seeder + DbContext (Scoped) for repositories and unit of work
        // AddDbContextFactory registers both automatically from EF Core 10 
        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.UseOpenIddict();
        });

        // 3. OpenIddict Core with EF Integration
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>();
            });

        return services;
    }

}
