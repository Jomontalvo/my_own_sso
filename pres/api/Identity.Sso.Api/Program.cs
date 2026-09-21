using Identity.Sso.Application;
using Identity.Sso.Persistence;
using Identity.Sso.Persistence.Context;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(config);

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();