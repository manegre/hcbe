using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;

namespace HcbeApi.Services;

public class NewsletterService : INewsletterService
{
    private static readonly HashSet<string> AllowedLanguages = new(StringComparer.OrdinalIgnoreCase) { "fr", "en" };
    private static readonly HashSet<string> AllowedSources = new(StringComparer.OrdinalIgnoreCase) { "home", "footer" };
    private const string GenericSuccessMessage = "Subscription successful";

    private readonly ApplicationDbContext _context;

    public NewsletterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<object>> SubscribeAsync(SubscribeNewsletterRequest request)
    {
        try
        {
            if (!request.ConsentAccepted)
            {
                return ApiResponse<object>.ErrorResponse("Consent is required");
            }

            var language = request.PreferredLanguage.Trim().ToLowerInvariant();
            if (!AllowedLanguages.Contains(language))
            {
                return ApiResponse<object>.ErrorResponse("Preferred language must be fr or en");
            }

            var source = request.Source.Trim().ToLowerInvariant();
            if (!AllowedSources.Contains(source))
            {
                return ApiResponse<object>.ErrorResponse("Source must be home or footer");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var fullName = request.FullName.Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return ApiResponse<object>.ErrorResponse("Full name is required");
            }

            var now = DateTime.UtcNow;
            var existing = await _context.NewsletterSubscriptions
                .FirstOrDefaultAsync(s => s.Email == email);

            if (existing == null)
            {
                _context.NewsletterSubscriptions.Add(new NewsletterSubscription
                {
                    Email = email,
                    FullName = fullName,
                    PreferredLanguage = language,
                    ConsentAcceptedAt = now,
                    IsActive = true,
                    Source = source,
                    UnsubscribeToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                LogConsent(email, "newsletter", "OptIn", source);
                await _context.SaveChangesAsync();
                return ApiResponse<object>.SuccessResponse(new { }, GenericSuccessMessage);
            }

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.FullName = fullName;
                existing.PreferredLanguage = language;
                existing.Source = source;
                existing.ConsentAcceptedAt = now;
                existing.UpdatedAt = now;
                LogConsent(email, "newsletter", "OptIn", source);
                await _context.SaveChangesAsync();
            }

            return ApiResponse<object>.SuccessResponse(new { }, GenericSuccessMessage);
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.ErrorResponse(
                "Failed to subscribe to newsletter",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse> UnsubscribeAsync(string token, Guid? campaignId = null)
    {
        if (string.IsNullOrWhiteSpace(token)) return ApiResponse.CreateError("Invalid unsubscribe token");
        var subscription = await _context.NewsletterSubscriptions
            .FirstOrDefaultAsync(item => item.UnsubscribeToken == token);
        if (subscription is null) return ApiResponse.CreateError("Subscription not found");
        var now = DateTime.UtcNow;
        if (subscription.IsActive) LogConsent(subscription.Email, "newsletter", "OptOut", campaignId.HasValue ? "campaign" : "unsubscribe-link");
        subscription.IsActive = false;
        subscription.UpdatedAt = now;
        if (campaignId.HasValue)
        {
            var delivery = await _context.NewsletterDeliveries.FirstOrDefaultAsync(item => item.CampaignId == campaignId && item.Recipient == subscription.Email);
            if (delivery is not null) delivery.UnsubscribedAtUtc ??= now;
        }
        await _context.SaveChangesAsync();
        return ApiResponse.CreateSuccess("You have been unsubscribed");
    }

    public async Task<ApiResponse<List<NewsletterSubscriptionDto>>> GetAllAsync(
        string? language = null,
        bool? isActive = null)
    {
        try
        {
            var query = _context.NewsletterSubscriptions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(language))
            {
                var normalized = language.Trim().ToLowerInvariant();
                query = query.Where(s => s.PreferredLanguage == normalized);
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return ApiResponse<List<NewsletterSubscriptionDto>>.SuccessResponse(
                subscriptions.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ApiResponse<List<NewsletterSubscriptionDto>>.ErrorResponse(
                "Failed to retrieve newsletter subscriptions",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<PagedResult<NewsletterSubscriptionDto>>> SearchAsync(
        int page,
        int pageSize,
        string? search,
        string? sort,
        string? language = null,
        bool? isActive = null)
    {
        try
        {
            (page, pageSize) = Pagination.Normalize(page, pageSize);
            var query = _context.NewsletterSubscriptions.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(language))
            {
                var normalized = language.Trim().ToLowerInvariant();
                query = query.Where(s => s.PreferredLanguage == normalized);
            }
            if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(term) || s.Email.ToLower().Contains(term));
            }

            query = sort?.ToLowerInvariant() switch
            {
                "name" => query.OrderBy(s => s.FullName),
                "oldest" => query.OrderBy(s => s.CreatedAt),
                _ => query.OrderByDescending(s => s.CreatedAt)
            };
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return ApiResponse<PagedResult<NewsletterSubscriptionDto>>.SuccessResponse(
                PagedResult<NewsletterSubscriptionDto>.Create(items.Select(MapToDto).ToList(), page, pageSize, total));
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<NewsletterSubscriptionDto>>.ErrorResponse(
                "Failed to retrieve newsletter subscriptions", new() { ex.Message });
        }
    }

    public async Task<ApiResponse<NewsletterSubscriptionDto>> UpdateActiveAsync(
        Guid id,
        UpdateNewsletterSubscriptionRequest request)
    {
        try
        {
            var subscription = await _context.NewsletterSubscriptions.FindAsync(id);
            if (subscription == null)
            {
                return ApiResponse<NewsletterSubscriptionDto>.ErrorResponse("Subscription not found");
            }

            if (subscription.IsActive != request.IsActive)
            {
                subscription.IsActive = request.IsActive;
                subscription.UpdatedAt = DateTime.UtcNow;
                LogConsent(subscription.Email, "newsletter", request.IsActive ? "OptIn" : "OptOut", "admin");
            }
            await _context.SaveChangesAsync();

            return ApiResponse<NewsletterSubscriptionDto>.SuccessResponse(MapToDto(subscription));
        }
        catch (Exception ex)
        {
            return ApiResponse<NewsletterSubscriptionDto>.ErrorResponse(
                "Failed to update newsletter subscription",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<string>> ExportActiveCsvAsync()
    {
        try
        {
            var subscriptions = await _context.NewsletterSubscriptions
                .Where(s => s.IsActive)
                .OrderBy(s => s.Email)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("FullName,Email,PreferredLanguage,Source,ConsentAcceptedAt,CreatedAt");

            foreach (var s in subscriptions)
            {
                sb.Append(EscapeCsv(s.FullName)).Append(',');
                sb.Append(EscapeCsv(s.Email)).Append(',');
                sb.Append(EscapeCsv(s.PreferredLanguage)).Append(',');
                sb.Append(EscapeCsv(s.Source)).Append(',');
                sb.Append(EscapeCsv(s.ConsentAcceptedAt.ToString("O", CultureInfo.InvariantCulture))).Append(',');
                sb.AppendLine(EscapeCsv(s.CreatedAt.ToString("O", CultureInfo.InvariantCulture)));
            }

            return ApiResponse<string>.SuccessResponse(sb.ToString());
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.ErrorResponse(
                "Failed to export newsletter subscriptions",
                new List<string> { ex.Message });
        }
    }

    public async Task<ApiResponse<List<CommunicationConsentEventDto>>> GetConsentHistoryAsync(int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);
        var items = await _context.CommunicationConsentEvents.AsNoTracking()
            .OrderByDescending(item => item.OccurredAtUtc).Take(limit).ToListAsync();
        return ApiResponse<List<CommunicationConsentEventDto>>.SuccessResponse(items.Select(item => new CommunicationConsentEventDto(
            item.Id, item.UserId, item.Email, item.Category, item.Action, item.Source, item.OccurredAtUtc)).ToList());
    }

    private void LogConsent(string email, string category, string action, string source) =>
        _context.CommunicationConsentEvents.Add(new CommunicationConsentEvent
        {
            Email = email, Category = category, Action = action, Source = source
        });

    private static NewsletterSubscriptionDto MapToDto(NewsletterSubscription subscription) =>
        new(
            subscription.Id,
            subscription.Email,
            subscription.FullName,
            subscription.PreferredLanguage,
            subscription.ConsentAcceptedAt,
            subscription.IsActive,
            subscription.Source,
            subscription.CreatedAt,
            subscription.UpdatedAt);

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
