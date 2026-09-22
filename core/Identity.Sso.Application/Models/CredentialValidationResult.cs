using Identity.Sso.Domain.Enums;

namespace Identity.Sso.Application.Models;

public sealed record CredentialValidationResult(
    CredentialValidationOutcome Outcome,
    string? SubjectId = null)
{
    public bool Succeeded => Outcome == CredentialValidationOutcome.Succeeded;
}
