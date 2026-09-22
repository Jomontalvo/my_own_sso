using Identity.Sso.Api.Security;
using Identity.Sso.Application.Behaviors.Authorization.Commands.ProcessToken;
using Identity.Sso.Application.Behaviors.Authorization.Queries.GetUserProfile;
using Identity.Sso.Application.Behaviors.Authorization.Queries.ProcessAuthorization;
using Identity.Sso.Application.Interfaces.OpenId;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;
using Identity.Sso.Domain.Errors;
using Identity.Sso.Domain.Shared;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Sso.Api.Endpoints;

/// <summary>
/// The OpenID Connect interaction endpoints. Discovery (<c>/.well-known/openid-configuration</c>) and JWKS
/// are served by the OpenIddict middleware itself and are intentionally not mapped here.
/// </summary>
public static class ConnectEndpoints
{
    public static RouteGroupBuilder MapConnectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/connect");

        group.MapMethods("/authorize", [HttpMethods.Get, HttpMethods.Post], HandleAuthorizeAsync)
             .WithName("Authorize")
               .WithTags("OpenID Connect")
               .WithSummary("Iniciar autorización interactiva")
               .WithDescription("Redirige al login o consentimiento y emite un código de autorización. Requiere Authorization Code + PKCE S256.")
             .DisableAntiforgery();

        group.MapPost("/token", HandleTokenAsync)
             .WithName("Token")
               .WithTags("OpenID Connect")
               .WithSummary("Emitir o renovar tokens")
               .WithDescription("Acepta application/x-www-form-urlencoded para authorization_code, refresh_token o client_credentials.")
             .DisableAntiforgery();

        group.MapMethods("/userinfo", [HttpMethods.Get, HttpMethods.Post], HandleUserInfoAsync)
             .WithName("UserInfo")
               .WithTags("OpenID Connect")
               .WithSummary("Obtener el perfil del usuario")
               .WithDescription("Devuelve claims vigentes filtrados por los scopes del access token. Requiere Bearer token de usuario.")
             .RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute
             {
                 AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
             })
             .DisableAntiforgery();

        group.MapMethods("/logout", [HttpMethods.Get, HttpMethods.Post], HandleLogoutAsync)
             .WithName("EndSession")
               .WithTags("OpenID Connect")
               .WithSummary("Finalizar la sesión central")
               .WithDescription("GET solicita confirmación; POST elimina la cookie SSO y valida post_logout_redirect_uri.")
             .DisableAntiforgery();

        return group;
    }

    private static async Task<IResult> HandleAuthorizeAsync(HttpContext context, IMediator mediator, IScopeStore scopeStore)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request could not be retrieved.");

        // The consent page redirects back here with this marker when the user rejects the request.
        if (context.Request.Query[Pages.ConsentModel.DeniedFlag] == "denied")
            return OAuthForbid(DomainErrors.Authorization.ConsentNotGranted);

        var session = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var subjectId = session.Succeeded ? session.Principal?.GetClaim(Claims.Subject) : null;

        var decision = await mediator.SendAsync(new ProcessAuthorizationQuery(
            SubjectId: subjectId,
            ClientId: request.ClientId ?? string.Empty,
            RequestedScopes: request.GetScopes(),
            PromptLogin: request.HasPromptValue(PromptValues.Login),
            PromptConsent: request.HasPromptValue(PromptValues.Consent),
            PromptNone: request.HasPromptValue(PromptValues.None),
            AuthenticatedAt: session.Properties?.IssuedUtc,
            MaxAge: request.MaxAge is { } maxAge ? TimeSpan.FromSeconds(maxAge) : null),
            context.RequestAborted);

        switch (decision.Outcome)
        {
            case AuthorizationOutcome.Challenge:
                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = BuildCurrentUrl(context) },
                    [IdentityConstants.ApplicationScheme]);

            case AuthorizationOutcome.Consent:
                return Results.Redirect($"/Consent?returnUrl={Uri.EscapeDataString(BuildCurrentUrl(context))}");

            case AuthorizationOutcome.Grant:
                var resources = await scopeStore.ListResourcesAsync(decision.GrantedScopes, context.RequestAborted);
                var principal = OpenIddictPrincipalFactory.ForUser(
                    decision.User!, decision.GrantedScopes, resources, decision.AuthorizationId);

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            default:
                return OAuthForbid(decision.Error!);
        }
    }

    private static async Task<IResult> HandleTokenAsync(HttpContext context, IMediator mediator, IScopeStore scopeStore)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request could not be retrieved.");

        var grantKind = ResolveGrantKind(request);

        string? subjectId = null;
        string? authorizationId = null;
        IReadOnlyCollection<string> grantedScopes = request.GetScopes();

        if (grantKind is TokenGrantKind.AuthorizationCode or TokenGrantKind.RefreshToken)
        {
            // OpenIddict already validated the code or refresh token; this recovers the principal it carries.
            var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            subjectId = result.Principal?.GetClaim(Claims.Subject);
            authorizationId = result.Principal?.GetAuthorizationId();
            grantedScopes = result.Principal?.GetScopes() ?? grantedScopes;
        }

        var decision = await mediator.SendAsync(new ProcessTokenCommand(
            grantKind, subjectId, request.ClientId, grantedScopes), context.RequestAborted);

        if (!decision.IsSuccess)
            return OAuthForbid(decision.Error!);

        var resources = await scopeStore.ListResourcesAsync(decision.GrantedScopes, context.RequestAborted);

        var principal = decision.User is not null
            ? OpenIddictPrincipalFactory.ForUser(decision.User, decision.GrantedScopes, resources, authorizationId)
            : OpenIddictPrincipalFactory.ForClient(decision.ClientId!, decision.GrantedScopes, resources);

        return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> HandleUserInfoAsync(HttpContext context, IMediator mediator)
    {
        var subjectId = context.User.GetClaim(Claims.Subject);
        if (string.IsNullOrEmpty(subjectId))
            return InvalidToken("The access token does not contain a subject.");

        var user = await mediator.SendAsync(new GetUserProfileQuery(subjectId), context.RequestAborted);
        if (user is null)
            return InvalidToken("The account associated with the access token no longer exists.");

        return Results.Ok(UserInfoBuilder.Build(user, context.User.GetScopes()));
    }

    private static async Task<IResult> HandleLogoutAsync(HttpContext context, SignInManager<Persistence.Models.ApplicationUser> signInManager)
    {
        // A GET must not terminate the session on its own: confirm it first (OIDC RP-Initiated Logout, §3).
        if (HttpMethods.IsGet(context.Request.Method))
            return Results.Redirect($"/Account/Logout?returnUrl={Uri.EscapeDataString(BuildCurrentUrl(context))}");

        await signInManager.SignOutAsync();

        // Lets OpenIddict validate post_logout_redirect_uri and honour the state parameter.
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }

    private static TokenGrantKind ResolveGrantKind(OpenIddictRequest request)
    {
        if (request.IsAuthorizationCodeGrantType()) return TokenGrantKind.AuthorizationCode;
        if (request.IsRefreshTokenGrantType()) return TokenGrantKind.RefreshToken;
        if (request.IsClientCredentialsGrantType()) return TokenGrantKind.ClientCredentials;
        return TokenGrantKind.Unsupported;
    }

    private static IResult OAuthForbid(Error error) =>
        Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error.Code,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = error.Description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    private static IResult InvalidToken(string description) =>
        Results.Challenge(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    private static string BuildCurrentUrl(HttpContext context)
    {
        var parameters = context.Request.HasFormContentType
            ? context.Request.Form.ToList()
            : context.Request.Query.ToList();

        return context.Request.PathBase + context.Request.Path + QueryString.Create(parameters);
    }
}
