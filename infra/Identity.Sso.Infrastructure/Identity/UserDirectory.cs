using Identity.Sso.Application.Interfaces.Identity;
using Identity.Sso.Application.Models;
using Identity.Sso.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.Sso.Infrastructure.Identity;

/// <summary>
/// Adapts ASP.NET Identity's <see cref="UserManager{TUser}"/> to the Application's <see cref="IUserDirectory"/> port.
/// </summary>
public sealed class UserDirectory(UserManager<ApplicationUser> userManager) : IUserDirectory
{
    public async Task<UserProfile?> FindBySubjectAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(subjectId);
        if (user is null)
            return null;

        var roles = await userManager.GetRolesAsync(user);

        return new UserProfile(
            SubjectId: user.Id,
            UserName: user.UserName ?? user.Id,
            Email: user.Email,
            EmailConfirmed: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            BirthDate: user.BirthDate.Value,
            TenantId: user.TenantId,
            ImageFileUrl: user.ImageFileUrl,
            IsActive: user.IsActive,
            Roles: [.. roles]);
    }
}
