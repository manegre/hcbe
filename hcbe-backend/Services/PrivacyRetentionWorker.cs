namespace HcbeApi.Services;

public sealed class PrivacyRetentionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PrivacyRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IPrivacyService>();
                var deletions = await service.ProcessDueDeletionsAsync(stoppingToken);
                var purged = await service.PurgeExpiredOperationalDataAsync(stoppingToken);
                if (deletions > 0 || purged > 0)
                    logger.LogInformation("Privacy maintenance processed {DeletionCount} deletions and purged {PurgeCount} records", deletions, purged);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Privacy retention maintenance failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
