using Identity.Sso.Domain.Enums;

namespace Identity.Sso.Application.Models;

public sealed record ClientApplication(
    string ClientId,
    string? DisplayName,
    ClientConsentType ConsentType);
