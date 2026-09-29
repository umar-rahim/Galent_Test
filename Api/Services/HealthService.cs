using Api.Services.Contracts;

namespace Api.Services;

public sealed class HealthService : IHealthService
{
    public HealthStatus GetStatus() => new("ok");
}
