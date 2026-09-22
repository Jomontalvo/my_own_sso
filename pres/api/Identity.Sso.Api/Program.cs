using DotNetEnv;
using Identity.Sso.Api.Endpoints;
using Identity.Sso.Api.Middleware;
using Identity.Sso.Application;
using Identity.Sso.Infrastructure;
using Identity.Sso.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
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

var config = builder.Configuration;

// 2. Clean Architecture layers, composed from the inside out.
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(config);
builder.Services.AddInfrastructureServices(config);

// 3. ASP.NET Identity + the OpenIddict server, both owned by the Infrastructure layer.
builder.Services.AddIdentityServices(config);
builder.Services.AddOpenIddictServer(config, builder.Environment);

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "SIGOB SSO Identity Provider";
        document.Info.Version = "v1";

        var documentationPath = Path.Combine(AppContext.BaseDirectory, "API.md");
        if (File.Exists(documentationPath))
            document.Info.Description = File.ReadAllText(documentationPath);

        return Task.CompletedTask;
    });
});

// 4. Rate limiting for the credential-facing surface.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("sso-credentials", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// 5. Correct client IP and scheme when running behind the Interop gateway or a reverse proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(Identity.Sso.Infrastructure.DependencyContainer.SsoCorsPolicy);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Discovery and JWKS are served by the OpenIddict middleware, so they are not mapped here.
app.MapHealthProbes();
app.MapConnectEndpoints().RequireRateLimiting("sso-credentials");
app.MapRazorPages().RequireRateLimiting("sso-credentials");

app.Run();

// Exposed so the integration test project can host the application through WebApplicationFactory<Program>.
public partial class Program;