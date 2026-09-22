namespace Identity.Sso.Application.Models;

/// <summary>
/// Layer-agnostic projection of an authenticated user. Replaces the former use of <c>ClaimsPrincipal</c>
/// in the Application contracts: claim construction belongs to the presentation layer.
/// </summary>
public sealed record UserProfile(
    string SubjectId,
    string UserName,
    string? Email,
    bool EmailConfirmed,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    Guid TenantId,
    string? ImageFileUrl,
    bool IsActive,
    IReadOnlyCollection<string> Roles)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
