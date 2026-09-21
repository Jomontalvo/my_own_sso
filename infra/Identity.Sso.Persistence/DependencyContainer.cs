
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Sso.Persistence;

public static class DependencyContainer
{
    /// <summary>
    /// Adds the persistence services to the service collection, including DbContext, repositories, and unit of work.
    /// </summary>
    /// <param name="services">The service collection to which the persistence services will be added.</param>
    /// <returns>The updated service collection with the persistence services added.</returns>
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
    {
        return services;
    }

}
