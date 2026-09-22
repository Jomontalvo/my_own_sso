namespace Identity.Sso.Application.Models;

public sealed record ScopeDescription(
    string Name,
    string? DisplayName,
    string? Description);
