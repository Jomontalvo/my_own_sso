using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Identity.Sso.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthProbes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        })
            .WithName("LivenessProbe")
            .WithTags("Health")
            .WithSummary("Check whether the SSO process is alive")
            .WithDescription("Kubernetes liveness probe. It does not check external dependencies.")
            .AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        })
            .WithName("ReadinessProbe")
            .WithTags("Health")
            .WithSummary("Check whether the SSO provider is ready to receive traffic")
            .WithDescription("Kubernetes readiness probe. It verifies access to the identity database.")
            .AllowAnonymous();

        return endpoints;
    }
}
