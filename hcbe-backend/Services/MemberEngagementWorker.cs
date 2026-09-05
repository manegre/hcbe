namespace HcbeApi.Services;

public sealed class MemberEngagementWorker(IServiceScopeFactory scopeFactory, ILogger<MemberEngagementWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IMemberEngagementService>();
                var reminders = await service.ProcessEventRemindersAsync(stoppingToken);
                var digests = await service.ProcessWeeklyDigestsAsync(stoppingToken);
                var journeys = await service.ProcessLifecycleJourneysAsync(stoppingToken);
                if (reminders + digests + journeys > 0) logger.LogInformation("Processed {Reminders} event reminders, {Digests} weekly digests and {Journeys} lifecycle journeys", reminders, digests, journeys);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Member engagement worker failed"); }
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
