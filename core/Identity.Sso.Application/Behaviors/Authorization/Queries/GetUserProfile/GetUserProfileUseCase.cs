using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.GetUserProfile;

public class GetUserProfileUseCase(IUserDirectory userDirectory)
    : IRequestHandler<GetUserProfileQuery, UserProfile?>
{
    public async Task<UserProfile?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken = default)
    {
        var user = await userDirectory.FindBySubjectAsync(request.SubjectId, cancellationToken);
        return user is { IsActive: true } ? user : null;
    }
}
