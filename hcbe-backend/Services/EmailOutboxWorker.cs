using HcbeApi.Data;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    private const int BatchSize = 10;
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                await Task.Delay(processed == 0 ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(250), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Email outbox batch failed");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;
        var staleLock = now.AddMinutes(-5);

        var messages = await context.EmailOutboxMessages
            .Where(message =>
                message.Attempts < MaxAttempts &&
                message.NextAttemptAtUtc <= now &&
                (message.Status == "Pending" || message.Status == "Failed" ||
                 (message.Status == "Processing" && message.LockedAtUtc < staleLock)))
            .OrderBy(message => message.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Status = "Processing";
            message.LockedAtUtc = DateTime.UtcNow;
            message.Attempts++;
            await context.SaveChangesAsync(cancellationToken);

            try
            {
                await sender.SendAsync(message.Recipient, message.Subject, message.HtmlBody, cancellationToken);
                message.Status = "Sent";
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.Status = message.Attempts >= MaxAttempts ? "DeadLetter" : "Failed";
                message.LastError = exception.Message.Length > 1000 ? exception.Message[..1000] : exception.Message;
                message.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Min(Math.Pow(2, message.Attempts), 60));
                logger.LogWarning(exception, "Email {OutboxMessageId} delivery attempt {Attempt} failed", message.Id, message.Attempts);
            }
            finally
            {
                message.LockedAtUtc = null;
                await UpdateCampaignAsync(context, message, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        return messages.Count;
    }

    private static async Task UpdateCampaignAsync(
        ApplicationDbContext context,
        EmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.RelatedEntityType != nameof(NewsletterCampaign) || message.RelatedEntityId == null) return;
        var campaign = await context.NewsletterCampaigns.FindAsync([message.RelatedEntityId.Value], cancellationToken);
        if (campaign == null) return;

        var delivery = await context.NewsletterDeliveries.FirstOrDefaultAsync(item =>
            item.CampaignId == campaign.Id && item.Recipient == message.Recipient, cancellationToken);
        if (delivery is not null)
        {
            delivery.EmailStatus = message.Status;
            delivery.FailureReason = message.Status == "DeadLetter" ? message.LastError : null;
        }

        var messages = context.EmailOutboxMessages.Where(item =>
            item.RelatedEntityType == nameof(NewsletterCampaign) && item.RelatedEntityId == campaign.Id);
        campaign.SentCount = await messages.CountAsync(item => item.Status == "Sent", cancellationToken);
        campaign.FailedCount = await messages.CountAsync(item => item.Status == "DeadLetter", cancellationToken);
        var finishedCount = campaign.SentCount + campaign.FailedCount;
        campaign.Status = finishedCount < campaign.RecipientCount
            ? "Sending"
            : campaign.FailedCount == 0
                ? "Sent"
                : campaign.SentCount == 0 ? "Failed" : "PartiallySent";
        if (finishedCount >= campaign.RecipientCount) campaign.SentAt = DateTime.UtcNow;
        campaign.LastError = message.Status == "DeadLetter" ? message.LastError : campaign.LastError;
    }
}
