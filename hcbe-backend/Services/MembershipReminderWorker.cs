namespace HcbeApi.Services;

public sealed class MembershipReminderWorker(IServiceScopeFactory scopeFactory, ILogger<MembershipReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processed = await scope.ServiceProvider.GetRequiredService<IFinanceService>().ProcessMembershipRemindersAsync(stoppingToken);
                if (processed > 0) logger.LogInformation("Queued {ReminderCount} membership reminders", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Membership reminder worker failed"); }
            try { await Task.Delay(TimeSpan.FromHours(12), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }
    }
}
