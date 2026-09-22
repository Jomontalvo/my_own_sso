using Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.Sso.Api.Endpoints;

public static class Authorization
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/connect");

        group.MapGet("/authorize", HandleAuthorizeAsync);
        group.MapPost("/authorize", HandleAuthorizeAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleAuthorizeAsync(
        HttpContext context,
        IMediator mediator)
    {
        var oidcRequest = context.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("Invalid OIDC request.");

        if (oidcRequest.ClientId == null)
            throw new InvalidOperationException("Invalid OIDC request: missing client ID.");

        var query = new ProcessAuthorizationQuery(context.User,
            oidcRequest.ClientId,
            oidcRequest.GetScopes(),
            oidcRequest.Prompt,
            oidcRequest.AcrValues,
            oidcRequest.Display);

        var result = await mediator.SendAsync(query);
        return result.Status switch
        {
            AuthorizationStatus.Success => Results.SignIn(
                result.ClaimsPrincipal!,
                authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme),

            AuthorizationStatus.ChallengeRequired => Results.Challenge(
                properties: new AuthenticationProperties
                {
                    RedirectUri = context.Request.PathBase + context.Request.Path + QueryString.Create(
                        context.Request.HasFormContentType ? [.. context.Request.Form] : context.Request.Query.ToList())
                },
                authenticationSchemes: [IdentityConstants.ApplicationScheme]),

            AuthorizationStatus.Forbidden => Results.Forbid(),

            _ => Results.BadRequest(new { error = result.ErrorMessage })
        };
    }
}
