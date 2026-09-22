using System.Security.Claims;
using Identity.Sso.Domain.Enums;

namespace Identity.Sso.Application.Models;

public class AuthorizationResult
{
    public AuthorizationStatus Status { get; set; }
    public ClaimsPrincipal? ClaimsPrincipal { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthorizationResult Success(ClaimsPrincipal principal) =>
        new() { Status = AuthorizationStatus.Success, ClaimsPrincipal = principal };

    public static AuthorizationResult RequireChallenge() =>
        new() { Status = AuthorizationStatus.ChallengeRequired };

    public static AuthorizationResult Forbid() =>
        new() { Status = AuthorizationStatus.Forbidden };

    public static AuthorizationResult Error(string message) =>
        new() { Status = AuthorizationStatus.Error, ErrorMessage = message };

}
