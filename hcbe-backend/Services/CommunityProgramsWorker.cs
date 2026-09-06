namespace HcbeApi.Services;

public sealed class CommunityProgramsWorker(IServiceScopeFactory scopeFactory, ILogger<CommunityProgramsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ICommunityProgramsService>();
                var response = await service.RunDueAutomationsAsync(false, stoppingToken);
                if (!response.Success) logger.LogWarning("Community programs automation failed: {Message}", response.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Community programs automation worker failed"); }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
