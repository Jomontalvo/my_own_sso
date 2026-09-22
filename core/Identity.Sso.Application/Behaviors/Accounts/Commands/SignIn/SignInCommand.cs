using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Accounts.Commands.SignIn;

public record SignInCommand(
    string UserName,
    string Password,
    bool RememberMe) : IRequest<CredentialValidationResult>;
