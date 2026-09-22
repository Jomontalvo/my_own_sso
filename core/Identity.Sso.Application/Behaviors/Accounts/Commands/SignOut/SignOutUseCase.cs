using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Accounts.Commands.SignOut;

public class SignOutUseCase(ICredentialValidator credentialValidator) : IRequestHandler<SignOutCommand>
{
    public Task Handle(SignOutCommand request, CancellationToken cancellationToken = default)
        => credentialValidator.SignOutAsync(cancellationToken);
}
