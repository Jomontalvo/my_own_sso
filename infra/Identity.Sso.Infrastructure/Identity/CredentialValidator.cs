using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Models;
using Identity.Sso.Domain.Enums;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.Sso.Infrastructure.Identity;

/// <summary>
/// Adapts <see cref="SignInManager{TUser}"/> to the Application's <see cref="ICredentialValidator"/> port.
/// Lockout accounting and the interactive session cookie are owned by ASP.NET Identity.
/// </summary>
public sealed class CredentialValidator(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ICredentialValidator
{
    public async Task<CredentialValidationResult> SignInAsync(
        string userName,
        string password,
        bool isPersistent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByNameAsync(userName)
            ?? await userManager.FindByEmailAsync(userName);

        // Same outcome for unknown and wrong-password to avoid user enumeration.
        if (user is null)
            return new CredentialValidationResult(CredentialValidationOutcome.InvalidCredentials);

        if (!user.IsActive)
            return new CredentialValidationResult(CredentialValidationOutcome.Inactive);

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true);

        return result switch
        {
            { Succeeded: true } => new CredentialValidationResult(CredentialValidationOutcome.Succeeded, user.Id),
            { IsLockedOut: true } => new CredentialValidationResult(CredentialValidationOutcome.LockedOut),
            { IsNotAllowed: true } => new CredentialValidationResult(CredentialValidationOutcome.NotAllowed),
            { RequiresTwoFactor: true } => new CredentialValidationResult(CredentialValidationOutcome.RequiresTwoFactor),
            _ => new CredentialValidationResult(CredentialValidationOutcome.InvalidCredentials)
        };
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return signInManager.SignOutAsync();
    }
}
