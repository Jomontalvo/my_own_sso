namespace Identity.Sso.Application.Models;

public sealed record ConsentContext(
    ClientApplication Client,
    IReadOnlyList<ScopeDescription> Scopes);
