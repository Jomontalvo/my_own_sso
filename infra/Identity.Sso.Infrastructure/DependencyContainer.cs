using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Infrastructure.Identity;
using Identity.Sso.Infrastructure.OpenId;
using Identity.Sso.Infrastructure.Options;
using Identity.Sso.Infrastructure.Seeding;
using Identity.Sso.Infrastructure.Security;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Identity.Sso.Infrastructure;

public static class DependencyContainer
{
    public const string SsoCorsPolicy = "SsoClients";

    /// <summary>
    /// Registers the adapters that implement the Application ports, plus the startup seeder.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<OidcOptions>()
            .Bind(configuration.GetSection(OidcOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICertificateProvider, CertificateProvider>();

        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<ICredentialValidator, CredentialValidator>();
        services.AddScoped<IClientApplicationStore, OpenIddictClientApplicationStore>();
        services.AddScoped<IAuthorizationStore, OpenIddictAuthorizationStore>();
        services.AddScoped<IScopeStore, OpenIddictScopeStore>();

        services.AddHostedService<IdentitySsoSeeder>();

        var origins = configuration
            .GetSection($"{OidcOptions.SectionName}:AllowedCorsOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(SsoCorsPolicy, policy =>
        {
            if (origins.Length == 0)
                return;

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }));

        return services;
    }

    /// <summary>
    /// Configures ASP.NET Identity and the interactive session cookie used by the authorization endpoint.
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var policy = configuration
            .GetSection(PasswordPolicyOptions.SectionName)
            .Get<PasswordPolicyOptions>() ?? new PasswordPolicyOptions();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = policy.RequireDigit;
            options.Password.RequireLowercase = policy.RequireLowercase;
            options.Password.RequireUppercase = policy.RequireUppercase;
            options.Password.RequireNonAlphanumeric = policy.RequireNonAlphanumeric;
            options.Password.RequiredLength = policy.RequiredLength;
            options.Password.RequiredUniqueChars = policy.RequiredUniqueChars;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = policy.MaxFailedAccessAttempts;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(policy.LockoutMinutes);

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;

            // OpenIddict reads the subject from this claim.
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "sigob.sso.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            // Lax: the authorization endpoint is reached through a top-level GET redirect.
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        return services;
    }

    /// <summary>
    /// Configures the OpenIddict server: endpoints, flows, scopes and signing material.
    /// </summary>
    public static IServiceCollection AddOpenIddictServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var oidc = configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>() ?? new OidcOptions();

        string[] standardScopes =
        [
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess
        ];

        services.AddOpenIddict()
            .AddCore(options => options
                .UseEntityFrameworkCore()
                .UseDbContext<ApplicationDbContext>())

            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetEndSessionEndpointUris("/connect/logout");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();
                options.AllowClientCredentialsFlow();

                // 'plain' offers no protection against code interception: only S256 is advertised and accepted.
                options.Configure(server =>
                    server.CodeChallengeMethods.Remove(OpenIddictConstants.CodeChallengeMethods.Plain));

                options.RegisterScopes([.. standardScopes, .. oidc.Scopes]);

                if (!string.IsNullOrWhiteSpace(oidc.Issuer))
                    options.SetIssuer(oidc.Issuer);

                // Resource servers can validate the access token locally while it stays unencrypted.
                if (oidc.DisableAccessTokenEncryption)
                    options.DisableAccessTokenEncryption();

                ConfigureCertificates(options, oidc, environment);

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();

                // Local hosts and the in-memory test server run over plain HTTP; production always requires TLS.
                if (!environment.IsProduction())
                    options.UseAspNetCore().DisableTransportSecurityRequirement();
            })

            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    private static void ConfigureCertificates(
        OpenIddictServerBuilder options,
        OidcOptions oidc,
        IHostEnvironment environment)
    {
        var provider = new CertificateProvider();

        if (oidc.SigningCertificate is { IsConfigured: true } signing)
            options.AddSigningCertificate(provider.Load(signing, "signing"));
        else if (!environment.IsProduction())
            options.AddDevelopmentSigningCertificate();
        else
            throw new InvalidOperationException(
                $"A persistent signing certificate must be configured under '{OidcOptions.SectionName}:SigningCertificate' in production.");

        if (oidc.EncryptionCertificate is { IsConfigured: true } encryption)
            options.AddEncryptionCertificate(provider.Load(encryption, "encryption"));
        else if (!environment.IsProduction())
            options.AddDevelopmentEncryptionCertificate();
        else
            throw new InvalidOperationException(
                $"A persistent encryption certificate must be configured under '{OidcOptions.SectionName}:EncryptionCertificate' in production.");
    }
}
