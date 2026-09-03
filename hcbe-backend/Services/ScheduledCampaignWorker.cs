namespace HcbeApi.Services;

public sealed class ScheduledCampaignWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledCampaignWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INewsletterCampaignService>();
                var count = await service.ProcessDueAsync(stoppingToken);
                if (count > 0) logger.LogInformation("Queued {CampaignCount} scheduled communications", count);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Scheduled communication processing failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
