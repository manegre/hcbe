namespace HcbeApi.Services;

public sealed class ScheduledCmsPublishingWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledCmsPublishingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ICmsContentService>();
                var count = await service.PublishDueAsync(stoppingToken);
                if (count > 0) logger.LogInformation("Published {ContentCount} scheduled CMS items", count);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Scheduled CMS publishing failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
