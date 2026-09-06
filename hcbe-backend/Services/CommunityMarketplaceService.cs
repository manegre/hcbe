using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class CommunityMarketplaceService(ApplicationDbContext context, IStripeConnectGateway stripe, IConfiguration configuration, ILogger<CommunityMarketplaceService> logger) : ICommunityMarketplaceService
{
    private static readonly HashSet<string> OrganizerReviewStatuses = new(StringComparer.OrdinalIgnoreCase) { OrganizerStatuses.Pending, OrganizerStatuses.Approved, OrganizerStatuses.Rejected, OrganizerStatuses.Suspended };
    private static readonly HashSet<string> AdReviewStatuses = new(StringComparer.OrdinalIgnoreCase) { "Draft", "Submitted", "Approved", "Rejected", "Paused" };
    private static readonly HashSet<string> AdPlacements = new(StringComparer.OrdinalIgnoreCase) { "Homepage", "News", "Services", "Events" };
    private string AppUrl => (configuration["PublicAppUrl"] ?? "https://hcbe.ca").TrimEnd('/');

    public async Task<ApiResponse<CommunityOrganizerDto?>> GetMyOrganizerAsync(Guid userId, CancellationToken ct)
    {
        var item = await context.CommunityOrganizers.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == userId, ct);
        return ApiResponse<CommunityOrganizerDto?>.SuccessResponse(item == null ? null : Map(item));
    }

    public async Task<ApiResponse<CommunityOrganizerDto>> SaveOrganizerAsync(Guid userId, UpsertOrganizerRequest request, CancellationToken ct)
    {
        if (!ValidUrl(request.WebsiteUrl, optional: true)) return ApiResponse<CommunityOrganizerDto>.ErrorResponse("Website must use https");
        var item = await context.CommunityOrganizers.SingleOrDefaultAsync(value => value.UserId == userId, ct);
        if (item == null) { item = new CommunityOrganizer { UserId = userId, Status = OrganizerStatuses.Pending }; context.CommunityOrganizers.Add(item); }
        item.DisplayName = request.DisplayName.Trim(); item.DisplayNameEn = Trim(request.DisplayNameEn); item.ContactEmail = request.ContactEmail.Trim().ToLowerInvariant();
        item.ContactPhone = Trim(request.ContactPhone); item.WebsiteUrl = Trim(request.WebsiteUrl); item.Description = Trim(request.Description); item.DescriptionEn = Trim(request.DescriptionEn); item.UpdatedAtUtc = DateTime.UtcNow;
        if (item.Status is OrganizerStatuses.Rejected or OrganizerStatuses.Suspended) item.Status = OrganizerStatuses.Pending;
        await context.SaveChangesAsync(ct); return ApiResponse<CommunityOrganizerDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse<IReadOnlyList<CommunityOrganizerDto>>> GetOrganizersAsync(CancellationToken ct)
    {
        var items = await context.CommunityOrganizers.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<CommunityOrganizerDto>>.SuccessResponse(items.Select(Map).ToList());
    }

    public async Task<ApiResponse<CommunityOrganizerDto>> ReviewOrganizerAsync(Guid id, ReviewOrganizerRequest request, CancellationToken ct)
    {
        var status = OrganizerReviewStatuses.FirstOrDefault(value => value.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status == null) return ApiResponse<CommunityOrganizerDto>.ErrorResponse("Invalid organizer status");
        var item = await context.CommunityOrganizers.SingleOrDefaultAsync(value => value.Id == id, ct);
        if (item == null) return ApiResponse<CommunityOrganizerDto>.ErrorResponse("Organizer not found");
        item.Status = status; item.ReviewNotes = Trim(request.ReviewNotes); item.ReviewedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<CommunityOrganizerDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse<OrganizerOnboardingDto>> CreateOnboardingAsync(Guid userId, CancellationToken ct)
    {
        var item = await context.CommunityOrganizers.SingleOrDefaultAsync(value => value.UserId == userId, ct);
        if (item == null || item.Status != OrganizerStatuses.Approved) return ApiResponse<OrganizerOnboardingDto>.ErrorResponse("Organizer approval is required before payment onboarding");
        if (!stripe.IsEnabled) return ApiResponse<OrganizerOnboardingDto>.ErrorResponse("Stripe Connect is not configured");
        try
        {
            if (string.IsNullOrWhiteSpace(item.StripeAccountId))
            {
                item.StripeAccountId = await stripe.CreateAccountAsync(item, ct);
                await context.SaveChangesAsync(ct);
            }
            var status = await stripe.GetStatusAsync(item.StripeAccountId, ct); ApplyStatus(item, status);
            if (item.StripeDetailsSubmitted && item.StripeChargesEnabled && item.StripePayoutsEnabled)
            { await context.SaveChangesAsync(ct); return ApiResponse<OrganizerOnboardingDto>.SuccessResponse(new($"{AppUrl}/espace-membre?section=organisateur", true)); }
            var page = $"{AppUrl}/espace-membre?section=organisateur";
            var link = await stripe.CreateOnboardingLinkAsync(item.StripeAccountId, page + "&stripe=complete", page + "&stripe=refresh", ct);
            await context.SaveChangesAsync(ct); return ApiResponse<OrganizerOnboardingDto>.SuccessResponse(new(link, false));
        }
        catch (Exception exception) { logger.LogWarning(exception, "Stripe onboarding failed for organizer {OrganizerId}", item.Id); return ApiResponse<OrganizerOnboardingDto>.ErrorResponse(exception.Message); }
    }

    public async Task<ApiResponse<CommunityOrganizerDto>> RefreshOrganizerAsync(Guid userId, CancellationToken ct)
    {
        var item = await context.CommunityOrganizers.SingleOrDefaultAsync(value => value.UserId == userId, ct);
        if (item == null) return ApiResponse<CommunityOrganizerDto>.ErrorResponse("Organizer not found");
        if (!string.IsNullOrWhiteSpace(item.StripeAccountId) && stripe.IsEnabled)
        {
            try { ApplyStatus(item, await stripe.GetStatusAsync(item.StripeAccountId, ct)); await context.SaveChangesAsync(ct); }
            catch (Exception exception) { logger.LogWarning(exception, "Stripe status refresh failed for organizer {OrganizerId}", item.Id); }
        }
        return ApiResponse<CommunityOrganizerDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse<IReadOnlyList<OrganizerEventDto>>> GetOrganizerEventsAsync(Guid userId, CancellationToken ct)
    {
        var organizer = await context.CommunityOrganizers.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == userId, ct);
        if (organizer == null) return ApiResponse<IReadOnlyList<OrganizerEventDto>>.SuccessResponse([]);
        var items = await context.Events.AsNoTracking().Include(item => item.TicketTiers).ThenInclude(item => item.OrderItems).ThenInclude(item => item.Order)
            .Where(item => item.CommunityOrganizerId == organizer.Id).OrderByDescending(item => item.CreatedAt).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<OrganizerEventDto>>.SuccessResponse(items.Select(MapOrganizerEvent).ToList());
    }

    public async Task<ApiResponse<OrganizerEventDto>> SaveOrganizerEventAsync(Guid? id, Guid userId, UpsertOrganizerEventRequest request, CancellationToken ct)
    {
        var organizer = await context.CommunityOrganizers.SingleOrDefaultAsync(value => value.UserId == userId && value.Status == OrganizerStatuses.Approved, ct);
        if (organizer == null) return ApiResponse<OrganizerEventDto>.ErrorResponse("An approved organizer profile is required");
        if (request.Date <= DateTime.UtcNow || request.EndDate <= request.Date) return ApiResponse<OrganizerEventDto>.ErrorResponse("Event dates are invalid");
        if (!new[] { "InPerson", "Online", "Hybrid" }.Contains(request.Format)) return ApiResponse<OrganizerEventDto>.ErrorResponse("Event format is invalid");
        if (!request.Currency.Equals("cad", StringComparison.OrdinalIgnoreCase)) return ApiResponse<OrganizerEventDto>.ErrorResponse("Only CAD ticket sales are supported");
        if (!ValidUrl(request.ImageUrl, true)) return ApiResponse<OrganizerEventDto>.ErrorResponse("Image link must use https");
        var item = id.HasValue ? await context.Events.Include(value => value.TicketTiers).SingleOrDefaultAsync(value => value.Id == id && value.CommunityOrganizerId == organizer.Id, ct) : null;
        if (id.HasValue && item == null) return ApiResponse<OrganizerEventDto>.ErrorResponse("Event not found");
        if (item != null && item.Status != "Draft") return ApiResponse<OrganizerEventDto>.ErrorResponse("Only draft organizer events can be edited");
        if (item == null)
        {
            item = new Event { CommunityOrganizerId = organizer.Id, SalesModel = "Community", TicketingEnabled = true, RegistrationMode = "Disabled", Status = "Draft", PlatformFeePercent = Math.Clamp(configuration.GetValue("CommunityMarketplace:PlatformFeePercent", 5), 0, 25) };
            context.Events.Add(item);
        }
        item.Title = request.Title.Trim(); item.TitleEn = Trim(request.TitleEn); item.Description = request.Description.Trim(); item.DescriptionEn = Trim(request.DescriptionEn);
        item.Date = request.Date.ToUniversalTime(); item.EndDate = request.EndDate?.ToUniversalTime(); item.Location = Trim(request.Location); item.LocationEn = Trim(request.LocationEn); item.Format = request.Format; item.ImageUrl = Trim(request.ImageUrl); item.UpdatedAt = DateTime.UtcNow;
        var tier = item.TicketTiers.FirstOrDefault();
        if (tier == null) { tier = new EventTicketTier { Event = item, Name = "Admission générale", NameEn = "General admission", SalesStartUtc = DateTime.UtcNow, DisplayOrder = 0 }; item.TicketTiers.Add(tier); }
        tier.PriceCents = request.PriceCents; tier.Currency = request.Currency.Trim().ToLowerInvariant(); tier.Quantity = request.TicketQuantity; tier.MaxPerOrder = Math.Min(20, request.TicketQuantity); tier.SalesEndUtc = request.Date.ToUniversalTime(); tier.IsActive = true; tier.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<OrganizerEventDto>.SuccessResponse(MapOrganizerEvent(item));
    }

    public async Task<ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>> GetActiveAdsAsync(string placement, string? language, string? province, string? zone, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await context.AdvertisingCampaigns.Where(item => item.Status == "Approved" && item.StartsAtUtc <= now && item.EndsAtUtc >= now).OrderBy(item => item.ImpressionCount).Take(50).ToListAsync(ct);
        var items = candidates.Where(item => Split(item.Placements).Contains(placement, StringComparer.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(item.TargetLanguage) || item.TargetLanguage.Equals(language, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(item.TargetProvince) || item.TargetProvince.Equals(province, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(item.TargetZone) || item.TargetZone.Equals(zone, StringComparison.OrdinalIgnoreCase))).Take(3).ToList();
        foreach (var item in items) item.ImpressionCount++;
        if (items.Count > 0) await context.SaveChangesAsync(ct);
        return ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>.SuccessResponse(items.Select(Map).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>> GetAdsAsync(CancellationToken ct)
    {
        var items = await context.AdvertisingCampaigns.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>.SuccessResponse(items.Select(Map).ToList());
    }

    public async Task<ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>> GetMyAdsAsync(Guid userId, CancellationToken ct)
    {
        var items = await context.AdvertisingCampaigns.AsNoTracking()
            .Where(item => item.SubmittedByUserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<AdvertisingCampaignDto>>.SuccessResponse(items.Select(Map).ToList());
    }

    public async Task<ApiResponse<AdvertisingCampaignDto>> SaveAdAsync(Guid? id, Guid userId, UpsertAdvertisingCampaignRequest request, CancellationToken ct)
    {
        if (!ValidUrl(request.DestinationUrl) || !ValidUrl(request.ImageUrl, true)) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Advertising links must use https");
        if (request.EndsAtUtc <= request.StartsAtUtc) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Campaign end must be after its start");
        var placements = request.Placements.SelectMany(Split).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (placements.Count == 0 || placements.Count > AdPlacements.Count || placements.Any(item => !AdPlacements.Contains(item)))
            return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Advertising placement is invalid");
        if (!string.IsNullOrWhiteSpace(request.TargetLanguage) && request.TargetLanguage is not ("fr" or "en"))
            return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Advertising language must be fr or en");
        if (!request.Currency.Equals("cad", StringComparison.OrdinalIgnoreCase)) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Only CAD advertising budgets are supported");
        var organizer = await context.CommunityOrganizers.SingleOrDefaultAsync(value => value.UserId == userId && value.Status == OrganizerStatuses.Approved, ct);
        if (organizer == null) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("An approved organizer profile is required");
        var item = id.HasValue ? await context.AdvertisingCampaigns.SingleOrDefaultAsync(value => value.Id == id && value.SubmittedByUserId == userId, ct) : null;
        if (id.HasValue && item == null) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Campaign not found");
        if (item == null) { item = new AdvertisingCampaign { SubmittedByUserId = userId, OrganizerId = organizer.Id }; context.AdvertisingCampaigns.Add(item); }
        Apply(item, request, placements); item.Status = "Submitted"; item.ReviewNotes = null; item.ReviewedAtUtc = null; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<AdvertisingCampaignDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse<AdvertisingCampaignDto>> ReviewAdAsync(Guid id, ReviewAdvertisingCampaignRequest request, CancellationToken ct)
    {
        var status = AdReviewStatuses.FirstOrDefault(value => value.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (status == null) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Invalid campaign status");
        var item = await context.AdvertisingCampaigns.SingleOrDefaultAsync(value => value.Id == id, ct);
        if (item == null) return ApiResponse<AdvertisingCampaignDto>.ErrorResponse("Campaign not found");
        item.Status = status; item.ReviewNotes = Trim(request.ReviewNotes); item.ReviewedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(ct); return ApiResponse<AdvertisingCampaignDto>.SuccessResponse(Map(item));
    }

    public async Task<Uri?> TrackAdClickAsync(Guid id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var item = await context.AdvertisingCampaigns.SingleOrDefaultAsync(value => value.Id == id && value.Status == "Approved" && value.StartsAtUtc <= now && value.EndsAtUtc >= now, ct);
        if (item == null || !Uri.TryCreate(item.DestinationUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return null;
        item.ClickCount++; await context.SaveChangesAsync(ct); return uri;
    }

    private static void ApplyStatus(CommunityOrganizer item, (bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled) status) { item.StripeDetailsSubmitted = status.DetailsSubmitted; item.StripeChargesEnabled = status.ChargesEnabled; item.StripePayoutsEnabled = status.PayoutsEnabled; item.UpdatedAtUtc = DateTime.UtcNow; }
    private static void Apply(AdvertisingCampaign item, UpsertAdvertisingCampaignRequest value, IReadOnlyList<string> placements) { item.AdvertiserName = value.AdvertiserName.Trim(); item.ContactEmail = value.ContactEmail.Trim().ToLowerInvariant(); item.Title = value.Title.Trim(); item.TitleEn = Trim(value.TitleEn); item.Body = value.Body.Trim(); item.BodyEn = Trim(value.BodyEn); item.ImageUrl = Trim(value.ImageUrl); item.DestinationUrl = value.DestinationUrl.Trim(); item.Placements = string.Join(',', placements); item.TargetLanguage = Trim(value.TargetLanguage); item.TargetProvince = Trim(value.TargetProvince); item.TargetZone = Trim(value.TargetZone); item.BudgetCents = value.BudgetCents; item.Currency = value.Currency.Trim().ToLowerInvariant(); item.StartsAtUtc = value.StartsAtUtc; item.EndsAtUtc = value.EndsAtUtc; }
    private static CommunityOrganizerDto Map(CommunityOrganizer item) => new(item.Id, item.UserId, item.DisplayName, item.DisplayNameEn, item.ContactEmail, item.ContactPhone, item.WebsiteUrl, item.Description, item.DescriptionEn, item.Status, item.ReviewNotes, !string.IsNullOrWhiteSpace(item.StripeAccountId), item.StripeDetailsSubmitted, item.StripeChargesEnabled, item.StripePayoutsEnabled, item.CreatedAtUtc, item.UpdatedAtUtc, item.ReviewedAtUtc);
    private static AdvertisingCampaignDto Map(AdvertisingCampaign item) => new(item.Id, item.OrganizerId, item.AdvertiserName, item.ContactEmail, item.Title, item.TitleEn, item.Body, item.BodyEn, item.ImageUrl, item.DestinationUrl, Split(item.Placements), item.TargetLanguage, item.TargetProvince, item.TargetZone, item.Status, item.ReviewNotes, item.BudgetCents, item.Currency, item.ImpressionCount, item.ClickCount, item.StartsAtUtc, item.EndsAtUtc, item.CreatedAtUtc, item.UpdatedAtUtc, item.ReviewedAtUtc);
    private static OrganizerEventDto MapOrganizerEvent(Event item) { var tier = item.TicketTiers.OrderBy(value => value.DisplayOrder).FirstOrDefault(); var sold = tier?.OrderItems.Where(value => value.Order.Status is TicketOrderStatuses.Paid or TicketOrderStatuses.PartiallyRefunded).Sum(value => value.Quantity) ?? 0; return new(item.Id, item.Title, item.TitleEn, item.Date, item.Location, item.Format, item.Status, tier?.PriceCents ?? 0, tier?.Currency ?? "cad", tier?.Quantity ?? 0, sold, item.CreatedAt); }
    private static List<string> Split(string? value) => (value ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidUrl(string? value, bool optional = false) => optional && string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
