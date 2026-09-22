using System.ComponentModel.DataAnnotations;
using Identity.Sso.Application.Behaviors.Accounts.Commands.SignIn;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Sso.Api.Pages.Account;

[AllowAnonymous]
public class LoginModel(IMediator mediator) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await mediator.SendAsync(
            new SignInCommand(Input.UserName, Input.Password, Input.RememberMe), cancellationToken);

        if (result.Succeeded)
            return LocalRedirect(ResolveReturnUrl());

        ErrorMessage = result.Outcome switch
        {
            CredentialValidationOutcome.LockedOut => "La cuenta está bloqueada temporalmente. Intente más tarde.",
            CredentialValidationOutcome.Inactive => "La cuenta está desactivada.",
            CredentialValidationOutcome.NotAllowed => "La cuenta no está habilitada para iniciar sesión.",
            CredentialValidationOutcome.RequiresTwoFactor => "Se requiere un segundo factor de autenticación.",
            // Same message for unknown user and wrong password: avoids user enumeration.
            _ => "Usuario o contraseña incorrectos."
        };

        return Page();
    }

    private string ResolveReturnUrl() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";

    public class InputModel
    {
        [Required]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(256, MinimumLength = 1)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
