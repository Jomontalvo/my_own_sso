using Identity.Sso.Application.Models;

namespace Identity.Sso.Application.Interfaces.Identity;

/// <summary>
/// Read access to the user store. Returns Application-owned projections so that neither
/// ASP.NET Identity types nor <c>ClaimsPrincipal</c> leak into the use cases.
/// </summary>
public interface IUserDirectory
{
    Task<UserProfile?> FindBySubjectAsync(string subjectId, CancellationToken cancellationToken = default);
}
