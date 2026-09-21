using DotNetEnv;
using Identity.Sso.Application;
using Identity.Sso.Persistence;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.Run();