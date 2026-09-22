using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;
using Identity.Sso.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Identity.Sso.Application.Behaviors.Accounts.Commands.SignIn;

public class SignInUseCase(
    ICredentialValidator credentialValidator,
    ILogger<SignInUseCase> logger)
    : IRequestHandler<SignInCommand, CredentialValidationResult>
{
    public async Task<CredentialValidationResult> Handle(
        SignInCommand request,
        CancellationToken cancellationToken = default)
    {
        var result = await credentialValidator.SignInAsync(
            request.UserName, request.Password, request.RememberMe, cancellationToken);

        if (result.Outcome != CredentialValidationOutcome.Succeeded)
        {
            // The user name is logged, never the password nor the reason shown to the caller.
            logger.LogInformation("Failed sign-in attempt for {UserName}: {Outcome}.", request.UserName, result.Outcome);
        }

        return result;
    }
}
