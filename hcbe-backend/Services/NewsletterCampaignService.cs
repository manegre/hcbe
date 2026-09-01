using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public class NewsletterCampaignService : INewsletterCampaignService
{
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
        var campaigns = await _context.NewsletterCampaigns.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt).ToListAsync();
        return ApiResponse<List<NewsletterCampaignDto>>.SuccessResponse(campaigns.Select(Map).ToList());
    }

    public async Task<ApiResponse<NewsletterCampaignDto>> CreateAsync(CreateNewsletterCampaignRequest request, Guid userId)
    {
        var campaign = new NewsletterCampaign
        {
            Subject = request.Subject.Trim(),
            SubjectEn = Normalize(request.SubjectEn),
            Body = request.Body.Trim(),
            BodyEn = Normalize(request.BodyEn),
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
        if (campaign.Status is "Sending" or "Sent")
            return ApiResponse<NewsletterCampaignDto>.ErrorResponse("Campaign has already been sent or is currently sending");

        var subscribers = await _context.NewsletterSubscriptions
            .Where(item => item.IsActive).OrderBy(item => item.Email).ToListAsync(cancellationToken);
        campaign.Status = "Queued";
        campaign.RecipientCount = subscribers.Count;
        campaign.SentCount = 0;
        campaign.FailedCount = 0;
        campaign.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);

        var publicApiUrl = (_configuration["PublicApiUrl"] ?? "http://localhost:8080").TrimEnd('/');
        foreach (var subscriber in subscribers)
        {
            if (string.IsNullOrWhiteSpace(subscriber.UnsubscribeToken))
                subscriber.UnsubscribeToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));

            var useEnglish = subscriber.PreferredLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);
            var subject = useEnglish && !string.IsNullOrWhiteSpace(campaign.SubjectEn) ? campaign.SubjectEn : campaign.Subject;
            var body = useEnglish && !string.IsNullOrWhiteSpace(campaign.BodyEn) ? campaign.BodyEn : campaign.Body;
            var unsubscribeUrl = $"{publicApiUrl}/api/newsletter/unsubscribe?token={Uri.EscapeDataString(subscriber.UnsubscribeToken)}";
            var email = _emailTemplates.Newsletter(subject!, body!, unsubscribeUrl, useEnglish);
            _emailOutbox.Enqueue(subscriber.Email, email.Subject, email.HtmlBody, nameof(NewsletterCampaign), campaign.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ApiResponse<NewsletterCampaignDto>.SuccessResponse(Map(campaign));
    }

    private static NewsletterCampaignDto Map(NewsletterCampaign item) => new(
        item.Id, item.Subject, item.SubjectEn, item.Body, item.BodyEn, item.Status,
        item.RecipientCount, item.SentCount, item.FailedCount, item.LastError, item.CreatedAt, item.SentAt);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
