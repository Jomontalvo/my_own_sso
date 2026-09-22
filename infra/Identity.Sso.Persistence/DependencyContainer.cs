using Identity.Sso.Application.Interfaces.Persistence;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Health;
using Identity.Sso.Persistence.UnitOfWorks;
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
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            });
            options.UseOpenIddict();
        });

        services.AddScoped<IUnitOfWork, UnitOfWorkEFCore<ApplicationDbContext>>();

        services.AddHealthChecks()
            .AddCheck<IdentityDatabaseHealthCheck>("identity_database", tags: ["ready"]);

        return services;
    }
}
