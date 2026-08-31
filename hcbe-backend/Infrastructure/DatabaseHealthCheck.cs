using HcbeApi.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HcbeApi.Infrastructure;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database connection is available.")
                : HealthCheckResult.Unhealthy("Database connection is unavailable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database readiness check failed.", exception);
        }
    }
}
