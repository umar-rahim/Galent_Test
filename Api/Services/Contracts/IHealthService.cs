namespace Api.Services.Contracts;

public interface IHealthService
{
    HealthStatus GetStatus();
}

public sealed record HealthStatus(string Status);
