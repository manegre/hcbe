using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class NewsletterCampaignService : INewsletterCampaignService
{
    private static readonly HashSet<string> Audiences = new(StringComparer.OrdinalIgnoreCase) { "Newsletter", "Members", "All" };
    private static readonly HashSet<string> PreferenceCategories = new(StringComparer.OrdinalIgnoreCase) { "newsletter", "events", "opportunities", "mentorship", "service" };
    private static readonly HashSet<string> SupportedChannels = new(StringComparer.OrdinalIgnoreCase) { "Email", "InApp", "Push" };
    private readonly ApplicationDbContext _context;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IEmailTemplateRenderer _emailTemplates;
    private readonly IConfiguration _configuration;
    private readonly IAppPushService _appPush;

    public NewsletterCampaignService(
        ApplicationDbContext context,
        IEmailOutbox emailOutbox,
        IEmailTemplateRenderer emailTemplates,
        IConfiguration configuration,
        IAppPushService appPush)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _emailTemplates = emailTemplates;
        _configuration = configuration;
        _appPush = appPush;
    }

    public async Task<ApiResponse<List<NewsletterCampaignDto>>> GetAllAsync()
    {
        var campaigns = await _context.NewsletterCampaigns.AsNoTracking().Include(item => item.Deliveries)
            .OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<NewsletterCampaignDto>>.SuccessResponse(campaigns.Select(Map).ToList());
    }

    public async Task<ApiResponse<NewsletterCampaignDto>> CreateAsync(CreateNewsletterCampaignRequest request, Guid userId)
    {
        var audience = Audiences.FirstOrDefault(item => item.Equals(request.Audience, StringComparison.OrdinalIgnoreCase));
        if (audience is null) return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Unsupported campaign audience");
        var category = PreferenceCategories.FirstOrDefault(item => item.Equals(request.PreferenceCategory, StringComparison.OrdinalIgnoreCase));
        if (category is null) return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Unsupported communication category");
        var channels = ParseChannels(request.Channels);
        if (channels.Count == 0) return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Select at least one communication channel");
        if (audience == "Newsletter" && channels.Any(channel => channel != "Email"))
            return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Public newsletter subscribers can only receive email campaigns");
        var campaign = new NewsletterCampaign
        {
            Subject = request.Subject.Trim(),
            SubjectEn = Normalize(request.SubjectEn),
            Body = request.Body.Trim(),
            BodyEn = Normalize(request.BodyEn),
            Audience = audience,
            Channels = string.Join(',', channels),
            PreferenceCategory = category,
            TargetProvince = Normalize(request.TargetProvince),
            TargetZone = Normalize(request.TargetZone),
            TargetLanguage = Normalize(request.TargetLanguage)?.ToLowerInvariant(),
            TargetInterest = Normalize(request.TargetInterest),
            TargetMembershipStatus = Normalize(request.TargetMembershipStatus),
            TargetAssociationId = request.TargetAssociationId,
            ScheduledAtUtc = request.ScheduledAtUtc?.ToUniversalTime(),
            Status = request.ScheduledAtUtc > DateTime.UtcNow ? "Scheduled" : "Draft",
            CreatedByUserId = userId
        };
        _context.NewsletterCampaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return ApiResponse<NewsletterCampaignDto>.SuccessResponse(Map(campaign));
    }

    public async Task<ApiResponse<CampaignAudiencePreviewDto>> PreviewAsync(CreateNewsletterCampaignRequest request, CancellationToken cancellationToken)
    {
        var channels = ParseChannels(request.Channels);
        if (channels.Count == 0) return ApiResponse<CampaignAudiencePreviewDto>.ErrorResponse("Select at least one communication channel");
        if (!Audiences.Contains(request.Audience)) return ApiResponse<CampaignAudiencePreviewDto>.ErrorResponse("Unsupported campaign audience");
        if (!PreferenceCategories.Contains(request.PreferenceCategory)) return ApiResponse<CampaignAudiencePreviewDto>.ErrorResponse("Unsupported communication category");
        if (request.Audience.Equals("Newsletter", StringComparison.OrdinalIgnoreCase) && channels.Any(channel => channel != "Email"))
            return ApiResponse<CampaignAudiencePreviewDto>.ErrorResponse("Public newsletter subscribers can only receive email campaigns");
        var campaign = ToPreviewCampaign(request, channels);
        var recipients = await BuildRecipientsAsync(campaign, cancellationToken);
        var memberIds = recipients.Values.Where(item => item.UserId.HasValue).Select(item => item.UserId!.Value).ToList();
        var pushReady = memberIds.Count == 0 ? 0 : await _context.WebPushSubscriptions.AsNoTracking()
            .Where(item => memberIds.Contains(item.UserId)).Select(item => item.UserId).Distinct().CountAsync(cancellationToken);
        return ApiResponse<CampaignAudiencePreviewDto>.SuccessResponse(new(
            recipients.Count,
            channels.Contains("Email") ? recipients.Count : 0,
            channels.Contains("InApp") ? memberIds.Count : 0,
            channels.Contains("Push") ? pushReady : 0));
    }

    public async Task<ApiResponse<List<CampaignDeliveryDto>>> GetDeliveriesAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await _context.NewsletterCampaigns.AnyAsync(item => item.Id == id, cancellationToken))
            return ApiResponse<List<CampaignDeliveryDto>>.ErrorResponse("Campaign not found");
        var deliveries = await _context.NewsletterDeliveries.AsNoTracking().Where(item => item.CampaignId == id)
            .OrderByDescending(item => item.QueuedAtUtc).Select(item => new CampaignDeliveryDto(
                item.Id, item.UserId, item.Recipient, item.PreferredLanguage, item.EmailStatus,
                item.InAppStatus, item.PushStatus, item.FailureReason, item.QueuedAtUtc,
                item.FirstOpenedAtUtc, item.OpenCount, item.UnsubscribedAtUtc)).ToListAsync(cancellationToken);
        return ApiResponse<List<CampaignDeliveryDto>>.SuccessResponse(deliveries);
    }

    public async Task<ApiResponse> SendTestAsync(Guid id, string email, CancellationToken cancellationToken)
    {
        var campaign = await _context.NewsletterCampaigns.FindAsync([id], cancellationToken);
        if (campaign is null) return ApiResponse.CreateError("Campaign not found");
        var english = campaign.TargetLanguage?.Equals("en", StringComparison.OrdinalIgnoreCase) == true;
        var subject = english && !string.IsNullOrWhiteSpace(campaign.SubjectEn) ? campaign.SubjectEn! : campaign.Subject;
        var body = english && !string.IsNullOrWhiteSpace(campaign.BodyEn) ? campaign.BodyEn! : campaign.Body;
        var publicUrl = (_configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var rendered = _emailTemplates.Newsletter($"[TEST] {subject}", body, $"{publicUrl}/espace-membre?section=preferences", english);
        _emailOutbox.Enqueue(email, rendered.Subject, rendered.HtmlBody, "NewsletterCampaignTest", campaign.Id);
        campaign.TestSentCount++;
        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse.CreateSuccess("Test campaign queued.");
    }

    public async Task<ApiResponse<NewsletterCampaignDto>> SendAsync(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await _context.NewsletterCampaigns.FindAsync(new object[] { id }, cancellationToken);
        if (campaign is null) return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Campaign not found");
        if (campaign.Status is "Sending" or "Queued" or "Sent")
            return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Campaign has already been sent or is currently sending");

        if (campaign.ScheduledAtUtc > DateTime.UtcNow)
        {
            campaign.Status = "Scheduled";
            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse<NewsletterCampaignDto>.SuccessResponse(Map(campaign));
        }

        await QueueCampaignAsync(campaign, cancellationToken);
        return ApiResponse<NewsletterCampaignDto>.SuccessResponse(Map(campaign));
    }

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var due = await _context.NewsletterCampaigns
            .Where(item => item.Status == "Scheduled" && item.ScheduledAtUtc <= DateTime.UtcNow)
            .OrderBy(item => item.ScheduledAtUtc).Take(10).ToListAsync(cancellationToken);
        foreach (var campaign in due) await QueueCampaignAsync(campaign, cancellationToken);
        return due.Count;
    }

    private async Task QueueCampaignAsync(NewsletterCampaign campaign, CancellationToken cancellationToken)
    {
        var recipients = await BuildRecipientsAsync(campaign, cancellationToken);
        var channels = ParseChannels(campaign.Channels);
        var publicApiUrl = (_configuration["PublicApiUrl"] ?? "http://localhost:8080").TrimEnd('/');

        campaign.Status = "Queued";
        campaign.RecipientCount = recipients.Count;
        campaign.SentCount = 0;
        campaign.FailedCount = 0;
        campaign.InAppSentCount = 0;
        campaign.PushSentCount = 0;
        campaign.PushFailedCount = 0;
        campaign.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            campaign.Status = "Sent";
            campaign.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var recipient in recipients.Values)
        {
            var useEnglish = recipient.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
            var subject = useEnglish && !string.IsNullOrWhiteSpace(campaign.SubjectEn) ? campaign.SubjectEn : campaign.Subject;
            var body = useEnglish && !string.IsNullOrWhiteSpace(campaign.BodyEn) ? campaign.BodyEn : campaign.Body;
            var email = _emailTemplates.Newsletter(subject!, body!, recipient.PreferencesUrl, useEnglish);
            var delivery = new NewsletterDelivery
            {
                CampaignId = campaign.Id, UserId = recipient.UserId, Recipient = recipient.Email,
                PreferredLanguage = recipient.Language,
                TrackingToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))
            };
            _context.NewsletterDeliveries.Add(delivery);
            if (channels.Contains("Email"))
            {
                delivery.EmailStatus = "Queued";
                var pixel = $"<img src=\"{publicApiUrl}/api/newsletter/track/open/{delivery.TrackingToken}.gif\" width=\"1\" height=\"1\" alt=\"\" style=\"display:block;border:0;width:1px;height:1px\" />";
                _emailOutbox.Enqueue(recipient.Email, email.Subject, email.HtmlBody + pixel, nameof(NewsletterCampaign), campaign.Id);
            }
            if (recipient.UserId.HasValue && channels.Contains("InApp"))
            {
                _context.Notifications.Add(new Notification { UserId = recipient.UserId, Type = "campaign", Title = subject!, Message = body!, Link = "/espace-membre?section=notifications" });
                delivery.InAppStatus = "Delivered";
                campaign.InAppSentCount++;
            }
            if (recipient.UserId.HasValue && channels.Contains("Push"))
            {
                var sent = await _appPush.SendToUserAsync(recipient.UserId.Value, subject!, body!, "/espace-membre?section=notifications", cancellationToken);
                delivery.PushStatus = sent > 0 ? "Delivered" : "Unavailable";
                campaign.PushSentCount += sent > 0 ? 1 : 0;
                campaign.PushFailedCount += sent > 0 ? 0 : 1;
                if (sent == 0) delivery.FailureReason = "No active push subscription or push consent.";
            }
        }

        if (!channels.Contains("Email"))
        {
            campaign.Status = "Sent";
            campaign.SentAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, CampaignRecipient>> BuildRecipientsAsync(NewsletterCampaign campaign, CancellationToken cancellationToken)
    {
        var recipients = new Dictionary<string, CampaignRecipient>(StringComparer.OrdinalIgnoreCase);
        var publicApiUrl = (_configuration["PublicApiUrl"] ?? "http://localhost:8080").TrimEnd('/');
        var publicUrl = (_configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
        if (campaign.Audience is "Newsletter" or "All")
        {
            var subscribers = await _context.NewsletterSubscriptions.Where(item => item.IsActive).OrderBy(item => item.Email).ToListAsync(cancellationToken);
            foreach (var subscriber in subscribers.Where(item => string.IsNullOrWhiteSpace(campaign.TargetLanguage) || item.PreferredLanguage == campaign.TargetLanguage))
            {
                if (string.IsNullOrWhiteSpace(subscriber.UnsubscribeToken)) subscriber.UnsubscribeToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
                recipients[subscriber.Email] = new(null, subscriber.Email, subscriber.PreferredLanguage,
                    $"{publicApiUrl}/api/newsletter/unsubscribe?token={Uri.EscapeDataString(subscriber.UnsubscribeToken)}&campaignId={campaign.Id}");
            }
        }
        if (campaign.Audience is not ("Members" or "All")) return recipients;
        var users = await _context.Users.AsNoTracking().Include(item => item.Member).Where(item => item.IsActive && item.MemberId != null).ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToList();
        var preferences = await _context.MemberPreferences.AsNoTracking().Where(item => userIds.Contains(item.UserId)).ToDictionaryAsync(item => item.UserId, cancellationToken);
        var standings = await _context.MembershipStandings.AsNoTracking().Where(item => userIds.Contains(item.UserId)).ToDictionaryAsync(item => item.UserId, cancellationToken);
        HashSet<Guid>? associationMemberIds = null;
        if (campaign.TargetAssociationId.HasValue)
            associationMemberIds = (await _context.AssociationMembers.AsNoTracking().Where(item => item.AssociationId == campaign.TargetAssociationId && item.Status == "Active").Select(item => item.MemberId).ToListAsync(cancellationToken)).ToHashSet();
        foreach (var user in users)
        {
            var member = user.Member!;
            if (!string.IsNullOrWhiteSpace(campaign.TargetProvince) && !string.Equals(member.Province, campaign.TargetProvince, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(campaign.TargetZone) && !string.Equals(member.Zone, campaign.TargetZone, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(campaign.TargetInterest) && !(member.Interests?.Contains(campaign.TargetInterest, StringComparison.OrdinalIgnoreCase) ?? false)) continue;
            if (associationMemberIds is not null && !associationMemberIds.Contains(member.Id)) continue;
            standings.TryGetValue(user.Id, out var standing);
            if (!string.IsNullOrWhiteSpace(campaign.TargetMembershipStatus) && !string.Equals(standing?.Status ?? MembershipStatuses.Inactive, campaign.TargetMembershipStatus, StringComparison.OrdinalIgnoreCase)) continue;
            preferences.TryGetValue(user.Id, out var preference);
            var language = preference?.PreferredLanguage ?? "fr";
            if (!string.IsNullOrWhiteSpace(campaign.TargetLanguage) && !string.Equals(language, campaign.TargetLanguage, StringComparison.OrdinalIgnoreCase)) continue;
            if (!Allows(preference, campaign.PreferenceCategory)) continue;
            recipients[user.Email] = new(user.Id, user.Email, language, $"{publicUrl}/espace-membre?section=preferences");
        }
        return recipients;
    }

    public async Task TrackOpenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var delivery = await _context.NewsletterDeliveries.FirstOrDefaultAsync(item => item.TrackingToken == token, cancellationToken);
        if (delivery is null) return;
        var now = DateTime.UtcNow;
        delivery.FirstOpenedAtUtc ??= now;
        delivery.LastOpenedAtUtc = now;
        delivery.OpenCount++;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static NewsletterCampaignDto Map(NewsletterCampaign item)
    {
        var opened = item.Deliveries.Count(delivery => delivery.FirstOpenedAtUtc.HasValue);
        var unsubscribed = item.Deliveries.Count(delivery => delivery.UnsubscribedAtUtc.HasValue);
        var openRate = item.SentCount == 0 ? 0 : Math.Round(opened * 100d / item.SentCount, 1);
        return new(
        item.Id, item.Subject, item.SubjectEn, item.Body, item.BodyEn, item.Status,
        item.RecipientCount, item.SentCount, item.FailedCount, item.LastError, item.CreatedAt, item.SentAt,
        item.Audience, item.Channels, item.PreferenceCategory, item.TargetProvince, item.TargetZone,
        item.TargetLanguage, item.TargetInterest, item.ScheduledAtUtc, item.TargetMembershipStatus,
        item.TargetAssociationId, opened, unsubscribed, openRate, item.InAppSentCount,
        item.PushSentCount, item.PushFailedCount, item.TestSentCount);
    }

    private static bool Allows(MemberPreference? preference, string category) => preference is { HasCompletedPreferences: true } && category switch
    {
        "events" => preference.EmailEvents,
        "opportunities" => preference.EmailOpportunities,
        "mentorship" => preference.EmailMentorship,
        "service" => preference.EmailServiceUpdates,
        _ => preference.EmailNewsletter
    };

    private static HashSet<string> ParseChannels(string? value) => (string.IsNullOrWhiteSpace(value) ? "Email" : value).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(channel => SupportedChannels.FirstOrDefault(item => item.Equals(channel, StringComparison.OrdinalIgnoreCase)))
        .Where(channel => channel is not null).Select(channel => channel!).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static NewsletterCampaign ToPreviewCampaign(CreateNewsletterCampaignRequest request, HashSet<string> channels) => new()
    {
        Subject = request.Subject, SubjectEn = request.SubjectEn, Body = request.Body, BodyEn = request.BodyEn,
        Audience = request.Audience, Channels = string.Join(',', channels), PreferenceCategory = request.PreferenceCategory,
        TargetProvince = Normalize(request.TargetProvince), TargetZone = Normalize(request.TargetZone),
        TargetLanguage = Normalize(request.TargetLanguage)?.ToLowerInvariant(), TargetInterest = Normalize(request.TargetInterest),
        TargetMembershipStatus = Normalize(request.TargetMembershipStatus), TargetAssociationId = request.TargetAssociationId
    };

    private sealed record CampaignRecipient(Guid? UserId, string Email, string Language, string PreferencesUrl);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
