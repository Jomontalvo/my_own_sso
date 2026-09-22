using Identity.Sso.Application.Models;

namespace Identity.Sso.Application.Interfaces.Identity;

/// <summary>
/// Validates user credentials and establishes the interactive session.
/// Lockout accounting and cookie issuance are infrastructure concerns handled by the implementation.
/// </summary>
public interface ICredentialValidator
{
    Task<CredentialValidationResult> SignInAsync(
        string userName,
        string password,
        bool isPersistent,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
