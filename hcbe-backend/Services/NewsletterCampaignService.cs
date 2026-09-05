using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class NewsletterCampaignService : INewsletterCampaignService
{
    private static readonly HashSet<string> Audiences = new(StringComparer.OrdinalIgnoreCase) { "Newsletter", "Members", "All" };
    private static readonly HashSet<string> PreferenceCategories = new(StringComparer.OrdinalIgnoreCase) { "newsletter", "events", "opportunities", "mentorship", "service" };
    private readonly ApplicationDbContext _context;
    private readonly IEmailOutbox _emailOutbox;
    private readonly IEmailTemplateRenderer _emailTemplates;
    private readonly IConfiguration _configuration;

    public NewsletterCampaignService(
        ApplicationDbContext context,
        IEmailOutbox emailOutbox,
        IEmailTemplateRenderer emailTemplates,
        IConfiguration configuration)
    {
        _context = context;
        _emailOutbox = emailOutbox;
        _emailTemplates = emailTemplates;
        _configuration = configuration;
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
        var campaign = new NewsletterCampaign
        {
            Subject = request.Subject.Trim(),
            SubjectEn = Normalize(request.SubjectEn),
            Body = request.Body.Trim(),
            BodyEn = Normalize(request.BodyEn),
            Audience = audience,
            PreferenceCategory = category,
            TargetProvince = Normalize(request.TargetProvince),
            TargetZone = Normalize(request.TargetZone),
            TargetLanguage = Normalize(request.TargetLanguage)?.ToLowerInvariant(),
            TargetInterest = Normalize(request.TargetInterest),
            ScheduledAtUtc = request.ScheduledAtUtc?.ToUniversalTime(),
            Status = request.ScheduledAtUtc > DateTime.UtcNow ? "Scheduled" : "Draft",
            CreatedByUserId = userId
        };
        _context.NewsletterCampaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return ApiResponse<NewsletterCampaignDto>.SuccessResponse(Map(campaign));
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
        var recipients = new Dictionary<string, CampaignRecipient>(StringComparer.OrdinalIgnoreCase);
        var publicApiUrl = (_configuration["PublicApiUrl"] ?? "http://localhost:8080").TrimEnd('/');
        var publicUrl = (_configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');

        if (campaign.Audience is "Newsletter" or "All")
        {
            var subscribers = await _context.NewsletterSubscriptions.Where(item => item.IsActive).OrderBy(item => item.Email).ToListAsync(cancellationToken);
            foreach (var subscriber in subscribers.Where(item => string.IsNullOrWhiteSpace(campaign.TargetLanguage) || item.PreferredLanguage == campaign.TargetLanguage))
            {
                if (string.IsNullOrWhiteSpace(subscriber.UnsubscribeToken))
                    subscriber.UnsubscribeToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
                recipients[subscriber.Email] = new(subscriber.Email, subscriber.PreferredLanguage,
                    $"{publicApiUrl}/api/newsletter/unsubscribe?token={Uri.EscapeDataString(subscriber.UnsubscribeToken)}&campaignId={campaign.Id}");
            }
        }

        if (campaign.Audience is "Members" or "All")
        {
            var users = await _context.Users.AsNoTracking().Include(item => item.Member)
                .Where(item => item.IsActive && item.MemberId != null).ToListAsync(cancellationToken);
            var userIds = users.Select(user => user.Id).ToList();
            var preferences = await _context.MemberPreferences.AsNoTracking()
                .Where(item => userIds.Contains(item.UserId))
                .ToDictionaryAsync(item => item.UserId, cancellationToken);
            foreach (var user in users)
            {
                var member = user.Member!;
                if (!string.IsNullOrWhiteSpace(campaign.TargetProvince) && !string.Equals(member.Province, campaign.TargetProvince, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(campaign.TargetZone) && !string.Equals(member.Zone, campaign.TargetZone, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(campaign.TargetInterest) && !(member.Interests?.Contains(campaign.TargetInterest, StringComparison.OrdinalIgnoreCase) ?? false)) continue;
                preferences.TryGetValue(user.Id, out var preference);
                var language = preference?.PreferredLanguage ?? "fr";
                if (!string.IsNullOrWhiteSpace(campaign.TargetLanguage) && !string.Equals(language, campaign.TargetLanguage, StringComparison.OrdinalIgnoreCase)) continue;
                if (!Allows(preference, campaign.PreferenceCategory)) continue;
                recipients[user.Email] = new(user.Email, language, $"{publicUrl}/espace-membre?section=preferences");
            }
        }

        campaign.Status = "Queued";
        campaign.RecipientCount = recipients.Count;
        campaign.SentCount = 0;
        campaign.FailedCount = 0;
        campaign.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var recipient in recipients.Values)
        {
            var useEnglish = recipient.Language.Equals("en", StringComparison.OrdinalIgnoreCase);
            var subject = useEnglish && !string.IsNullOrWhiteSpace(campaign.SubjectEn) ? campaign.SubjectEn : campaign.Subject;
            var body = useEnglish && !string.IsNullOrWhiteSpace(campaign.BodyEn) ? campaign.BodyEn : campaign.Body;
            var email = _emailTemplates.Newsletter(subject!, body!, recipient.PreferencesUrl, useEnglish);
            var delivery = new NewsletterDelivery
            {
                CampaignId = campaign.Id, Recipient = recipient.Email,
                TrackingToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))
            };
            _context.NewsletterDeliveries.Add(delivery);
            var pixel = $"<img src=\"{publicApiUrl}/api/newsletter/track/open/{delivery.TrackingToken}.gif\" width=\"1\" height=\"1\" alt=\"\" style=\"display:block;border:0;width:1px;height:1px\" />";
            _emailOutbox.Enqueue(recipient.Email, email.Subject, email.HtmlBody + pixel, nameof(NewsletterCampaign), campaign.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
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
        item.Audience, item.PreferenceCategory, item.TargetProvince, item.TargetZone,
        item.TargetLanguage, item.TargetInterest, item.ScheduledAtUtc, opened, unsubscribed, openRate);
    }

    private static bool Allows(MemberPreference? preference, string category) => preference is { HasCompletedPreferences: true } && category switch
    {
        "events" => preference.EmailEvents,
        "opportunities" => preference.EmailOpportunities,
        "mentorship" => preference.EmailMentorship,
        "service" => preference.EmailServiceUpdates,
        _ => preference.EmailNewsletter
    };

    private sealed record CampaignRecipient(string Email, string Language, string PreferencesUrl);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
