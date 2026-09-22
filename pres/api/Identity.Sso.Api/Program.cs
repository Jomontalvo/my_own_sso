using DotNetEnv;
using Identity.Sso.Application;
using Identity.Sso.Persistence;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Scalar.AspNetCore;
using static System.Environment;

var builder = WebApplication.CreateBuilder(args);

// 1. Load environment variables from .env file in development environment, if not running in Docker.
if (builder.Environment.IsDevelopment())
{

    // Try loading .env file only if not running in Docker, as in Docker we expect environment variables 
    // to be set through other means (e.g., docker-compose, Kubernetes secrets, etc.)
    var isDocker = GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    if (!isDocker)
    {
        var current = Directory.GetCurrentDirectory();
        var envPath = ".env";

        for (var depth = 0; depth < 10; depth++)
        {
            var candidate = Path.GetFullPath(envPath, current);
            if (File.Exists(candidate))
            {
                envPath = candidate;
                break;
            }
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Cannot traverse to root.");
        }

        if (File.Exists(envPath))
            Env.Load(envPath);
    }
}
builder.Configuration.AddEnvironmentVariables();

// 2. Register application and persistence services
var config = builder.Configuration;
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(config);

// 3. Add identity services
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    var passwordPolicySection = config.GetSection("Identity:PasswordPolicy");
    options.Password.RequireDigit = passwordPolicySection.GetValue<bool>("RequireDigit");
    options.Password.RequireLowercase = passwordPolicySection.GetValue<bool>("RequireLowercase");
    options.Password.RequireNonAlphanumeric = passwordPolicySection.GetValue<bool>("RequireNonAlphanumeric");
    options.Password.RequireUppercase = passwordPolicySection.GetValue<bool>("RequireUppercase");
    options.Password.RequiredLength = passwordPolicySection.GetValue<int>("RequiredLength");
    options.Password.RequiredUniqueChars = passwordPolicySection.GetValue<int>("RequiredUniqueChars");
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization();

// 3. OpenIddict Core with EF Integration
var defaultScopes = new[]
{
    OpenIddictConstants.Scopes.OpenId,
    OpenIddictConstants.Scopes.Profile,
    OpenIddictConstants.Scopes.Email,
    OpenIddictConstants.Scopes.Roles,
    OpenIddictConstants.Scopes.OfflineAccess
};
var customScopes = builder.Configuration
    .GetSection("OidcConfig:Scopes")
    .Get<string[]>() ?? [];
var allScopes = defaultScopes.Concat(customScopes).ToArray();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        // Standard OIDC Endpoint routes
        options.SetAuthorizationEndpointUris("/connect/authorize")
                .SetTokenEndpointUris("/connect/token")
                .SetUserInfoEndpointUris("/connect/userinfo")
                .SetEndSessionEndpointUris("/connect/logout");

        // Enabled OIDC / OAuth 2.0 Flows
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange(); // PKCE required for security

        options.AllowRefreshTokenFlow();      // Issuance and renewal with Refresh Tokens
        options.AllowClientCredentialsFlow(); // Machine-to-Machine (M2M) communication

        // Register supported Scopes
        options.RegisterScopes(allScopes);

        // Development certificates (For production, replace with persistent X.509 certificates)
        options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();

        // Integration with ASP.NET Core and enabling Passthrough
        options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        // Allow validating tokens within the same API if it exposes protected endpoints
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Run();