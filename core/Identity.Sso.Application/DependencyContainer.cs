using FluentValidation;
using Identity.Sso.Application.Behaviors.Accounts.Commands.SignIn;
using Identity.Sso.Application.Behaviors.Accounts.Commands.SignOut;
using Identity.Sso.Application.Behaviors.Authorization.Commands.GrantConsent;
using Identity.Sso.Application.Behaviors.Authorization.Commands.ProcessToken;
using Identity.Sso.Application.Behaviors.Authorization.Queries.GetConsentContext;
using Identity.Sso.Application.Behaviors.Authorization.Queries.GetUserProfile;
using Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;
using Identity.Sso.Application.Models;
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

        services.TryAddSingletonTimeProvider();

        services.AddTransient<IMediator, BasicMediator>();
        services.AddValidatorsFromAssemblyContaining<BasicMediator>();

        // BasicMediator resolves handlers through GetRequiredService: every handler must be registered explicitly.
        services.AddScoped<IRequestHandler<ProcessAuthorizationQuery, AuthorizationDecision>, ProcessAuthorizationUseCase>();
        services.AddScoped<IRequestHandler<ProcessTokenCommand, TokenDecision>, ProcessTokenUseCase>();
        services.AddScoped<IRequestHandler<GetUserProfileQuery, UserProfile?>, GetUserProfileUseCase>();
        services.AddScoped<IRequestHandler<GetConsentContextQuery, ConsentContext>, GetConsentContextUseCase>();
        services.AddScoped<IRequestHandler<GrantConsentCommand, string>, GrantConsentUseCase>();
        services.AddScoped<IRequestHandler<SignInCommand, CredentialValidationResult>, SignInUseCase>();
        services.AddScoped<IRequestHandler<SignOutCommand>, SignOutUseCase>();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);
    }
}

