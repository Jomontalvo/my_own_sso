using System.Security.Claims;
using Identity.Sso.Application.Behaviors.Authorization.Commands.GrantConsent;
using Identity.Sso.Application.Behaviors.Authorization.Queries.GetConsentContext;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Sso.Api.Pages;

[Authorize]
public class ConsentModel(IMediator mediator) : PageModel
{
    /// <summary>Marker read back by the authorization endpoint when the user rejects the request.</summary>
    public const string DeniedFlag = "sigob_consent";

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = string.Empty;

    public string ClientDisplayName { get; private set; } = string.Empty;

    public IReadOnlyList<ScopeDescription> Scopes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!TryParseAuthorizationRequest(out var clientId, out var scopes))
            return BadRequest();

        var context = await mediator.SendAsync(new GetConsentContextQuery(clientId, scopes), cancellationToken);

        ClientDisplayName = context.Client.DisplayName ?? context.Client.ClientId;
        Scopes = context.Scopes;

        return Page();
    }

    public async Task<IActionResult> OnPostAcceptAsync(CancellationToken cancellationToken)
    {
        if (!TryParseAuthorizationRequest(out var clientId, out var scopes))
            return BadRequest();

        var subjectId = User.FindFirstValue(Claims.Subject);
        if (string.IsNullOrEmpty(subjectId))
            return Forbid();

        await mediator.SendAsync(new GrantConsentCommand(subjectId, clientId, scopes), cancellationToken);

        return Redirect(ReturnUrl);
    }

    public IActionResult OnPostDeny()
    {
        if (!TryParseAuthorizationRequest(out _, out _))
            return BadRequest();

        // The authorization endpoint turns this into an access_denied error routed to the client's redirect_uri.
        return Redirect(QueryHelpers.AddQueryString(ReturnUrl, DeniedFlag, "denied"));
    }

    private bool TryParseAuthorizationRequest(out string clientId, out string[] scopes)
    {
        clientId = string.Empty;
        scopes = [];

        if (string.IsNullOrEmpty(ReturnUrl) || !Url.IsLocalUrl(ReturnUrl))
            return false;

        var separator = ReturnUrl.IndexOf('?');
        if (separator < 0)
            return false;

        var parameters = QueryHelpers.ParseQuery(ReturnUrl[separator..]);

        clientId = parameters.TryGetValue(Parameters.ClientId, out var client) ? client.ToString() : string.Empty;
        scopes = parameters.TryGetValue(Parameters.Scope, out var scope)
            ? scope.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

        return !string.IsNullOrEmpty(clientId) && scopes.Length > 0;
    }
}
