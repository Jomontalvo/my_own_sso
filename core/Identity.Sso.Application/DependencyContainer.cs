using FluentValidation;
using Identity.Sso.Application.Utils.Mapper;
using Identity.Sso.Application.Utils.Mediator;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Sso.Application;

public static class DependencyContainer
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        MapperConfig.RegisterAppMappings();
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddTransient<IMediator, BasicMediator>();
        services.AddValidatorsFromAssemblyContaining<BasicMediator>();

        return services;
    }

}
