using System.Text.RegularExpressions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed partial class CmsContentService(
    ApplicationDbContext context,
    ICmsContentNotifier notifier) : ICmsContentService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text", "richtext", "image", "url", "seo"
    };

    public async Task<ApiResponse<CmsPublishedBundleDto>> GetPublishedAsync()
    {
        var items = await context.CmsContentItems.AsNoTracking()
            .Where(item => item.IsPublished)
            .OrderBy(item => item.Key)
            .ToListAsync();

        var publishedAt = items.Count == 0 ? null : items.Max(item => item.PublishedAt);
        var bundle = new CmsPublishedBundleDto(
            ToBundleVersion(publishedAt),
            publishedAt,
            items.Select(item => new CmsPublishedContentDto(
                item.Key,
                item.ContentType,
                item.PublishedValueFr,
                item.PublishedValueEn,
                item.Version)).ToList());

        return ApiResponse<CmsPublishedBundleDto>.SuccessResponse(bundle);
    }

    public async Task<ApiResponse<List<CmsContentItemDto>>> GetAdminItemsAsync(string? page = null)
    {
        var query = context.CmsContentItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(page))
            query = query.Where(item => item.Page == page.Trim().ToLowerInvariant());

        var items = await query
            .OrderBy(item => item.Page)
            .ThenBy(item => item.Section)
            .ThenBy(item => item.Key)
            .ToListAsync();

        return ApiResponse<List<CmsContentItemDto>>.SuccessResponse(items.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<CmsContentItemDto>> UpsertAsync(UpsertCmsContentRequest request, Guid? userId)
    {
        var key = request.Key.Trim().ToLowerInvariant();
        if (!ContentKeyRegex().IsMatch(key))
            return ApiResponse<CmsContentItemDto>.ErrorResponse("Content key may only contain letters, numbers, dots, dashes, and underscores");

        var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "text" : request.ContentType.Trim().ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType))
            return ApiResponse<CmsContentItemDto>.ErrorResponse("Unsupported content type");

        var item = await context.CmsContentItems.FirstOrDefaultAsync(existing => existing.Key == key);
        if (item is null)
        {
            var inferred = InferLocation(key);
            item = new CmsContentItem
            {
                Key = key,
                Page = NormalizeLocation(request.Page, inferred.Page),
                Section = NormalizeLocation(request.Section, inferred.Section),
                ContentType = contentType
            };
            context.CmsContentItems.Add(item);
        }
        else
        {
            item.Page = NormalizeLocation(request.Page, item.Page);
            item.Section = NormalizeLocation(request.Section, item.Section);
            item.ContentType = contentType;
        }

        item.Label = Normalize(request.Label);
        item.DraftValueFr = Normalize(request.ValueFr);
        item.DraftValueEn = Normalize(request.ValueEn);
        item.UpdatedByUserId = userId;
        item.UpdatedAt = DateTime.UtcNow;

        item.ScheduledPublishAtUtc = request.ScheduledPublishAtUtc > DateTime.UtcNow
            ? request.ScheduledPublishAtUtc.Value.ToUniversalTime()
            : null;

        if (request.Publish)
            PublishEntity(item, userId, DateTime.UtcNow);

        await context.SaveChangesAsync();
        if (request.Publish)
            await notifier.NotifyPublishedAsync(ToBundleVersion(item.PublishedAt));

        return ApiResponse<CmsContentItemDto>.SuccessResponse(MapToDto(item));
    }

    public async Task<ApiResponse<CmsContentItemDto>> PublishAsync(Guid id, Guid? userId)
    {
        var item = await context.CmsContentItems.FindAsync(id);
        if (item is null) return ApiResponse<CmsContentItemDto>.ErrorResponse("Content item not found");

        PublishEntity(item, userId, DateTime.UtcNow);
        await context.SaveChangesAsync();
        await notifier.NotifyPublishedAsync(ToBundleVersion(item.PublishedAt));
        return ApiResponse<CmsContentItemDto>.SuccessResponse(MapToDto(item));
    }

    public async Task<ApiResponse<CmsPublishResultDto>> PublishAllAsync(Guid? userId)
    {
        var items = await context.CmsContentItems
            .Where(item => !item.IsPublished ||
                item.DraftValueFr != item.PublishedValueFr ||
                item.DraftValueEn != item.PublishedValueEn)
            .Where(item => item.ScheduledPublishAtUtc == null || item.ScheduledPublishAtUtc <= DateTime.UtcNow)
            .ToListAsync();

        var publishedAt = DateTime.UtcNow;
        foreach (var item in items) PublishEntity(item, userId, publishedAt);
        await context.SaveChangesAsync();

        var version = ToBundleVersion(publishedAt);
        if (items.Count > 0) await notifier.NotifyPublishedAsync(version);
        return ApiResponse<CmsPublishResultDto>.SuccessResponse(
            new CmsPublishResultDto(items.Count, version, publishedAt));
    }

    public async Task<ApiResponse<List<CmsContentRevisionDto>>> GetRevisionsAsync(Guid id)
    {
        if (!await context.CmsContentItems.AnyAsync(item => item.Id == id))
            return ApiResponse<List<CmsContentRevisionDto>>.ErrorResponse("Content item not found");

        var revisions = await context.CmsContentRevisions.AsNoTracking()
            .Where(revision => revision.CmsContentItemId == id)
            .OrderByDescending(revision => revision.Version)
            .Select(revision => new CmsContentRevisionDto(
                revision.Id,
                revision.Version,
                revision.ValueFr,
                revision.ValueEn,
                revision.PublishedByUserId,
                revision.PublishedAt))
            .ToListAsync();

        return ApiResponse<List<CmsContentRevisionDto>>.SuccessResponse(revisions);
    }

    public async Task<ApiResponse<CmsContentItemDto>> RollbackAsync(Guid id, int version, Guid? userId)
    {
        var item = await context.CmsContentItems.FindAsync(id);
        if (item is null) return ApiResponse<CmsContentItemDto>.ErrorResponse("Content item not found");
        var revision = await context.CmsContentRevisions.AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.CmsContentItemId == id && entry.Version == version);
        if (revision is null) return ApiResponse<CmsContentItemDto>.ErrorResponse("Content revision not found");

        item.DraftValueFr = revision.ValueFr;
        item.DraftValueEn = revision.ValueEn;
        PublishEntity(item, userId, DateTime.UtcNow);
        await context.SaveChangesAsync();
        await notifier.NotifyPublishedAsync(ToBundleVersion(item.PublishedAt));
        return ApiResponse<CmsContentItemDto>.SuccessResponse(MapToDto(item));
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var item = await context.CmsContentItems.FindAsync(id);
        if (item is null) return ApiResponse.CreateError("Content item not found");
        var wasPublished = item.IsPublished;
        context.CmsContentItems.Remove(item);
        await context.SaveChangesAsync();
        if (wasPublished) await notifier.NotifyPublishedAsync(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return ApiResponse.CreateSuccess("Content override deleted");
    }

    public async Task<int> PublishDueAsync(CancellationToken cancellationToken)
    {
        var items = await context.CmsContentItems
            .Where(item => item.ScheduledPublishAtUtc != null && item.ScheduledPublishAtUtc <= DateTime.UtcNow)
            .OrderBy(item => item.ScheduledPublishAtUtc).Take(50).ToListAsync(cancellationToken);
        if (items.Count == 0) return 0;
        var publishedAt = DateTime.UtcNow;
        foreach (var item in items) PublishEntity(item, item.UpdatedByUserId, publishedAt);
        await context.SaveChangesAsync(cancellationToken);
        await notifier.NotifyPublishedAsync(ToBundleVersion(publishedAt), cancellationToken);
        return items.Count;
    }

    private void PublishEntity(CmsContentItem item, Guid? userId, DateTime publishedAt)
    {
        item.PublishedValueFr = item.DraftValueFr;
        item.PublishedValueEn = item.DraftValueEn;
        item.IsPublished = true;
        item.Version += 1;
        item.PublishedAt = publishedAt;
        item.PublishedByUserId = userId;
        item.UpdatedAt = publishedAt;
        item.ScheduledPublishAtUtc = null;
        context.CmsContentRevisions.Add(new CmsContentRevision
        {
            CmsContentItemId = item.Id,
            Version = item.Version,
            ValueFr = item.PublishedValueFr,
            ValueEn = item.PublishedValueEn,
            PublishedByUserId = userId,
            PublishedAt = publishedAt
        });
    }

    private static CmsContentItemDto MapToDto(CmsContentItem item) => new(
        item.Id,
        item.Key,
        item.Page,
        item.Section,
        item.ContentType,
        item.Label,
        item.DraftValueFr,
        item.DraftValueEn,
        item.PublishedValueFr,
        item.PublishedValueEn,
        item.IsPublished,
        !item.IsPublished || item.DraftValueFr != item.PublishedValueFr || item.DraftValueEn != item.PublishedValueEn,
        item.Version,
        item.UpdatedAt,
        item.PublishedAt,
        item.ScheduledPublishAtUtc);

    private static (string Page, string Section) InferLocation(string key)
    {
        var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var page = segments.Length > 1 ? segments[1] : "global";
        if (page is "nav" or "footer" or "brand" or "theme" or "cookies" or "newsletter") page = "global";
        var sectionIndex = page == "global" ? 1 : 2;
        var section = segments.Length > sectionIndex ? segments[sectionIndex] : "general";
        return (page, section);
    }

    private static string NormalizeLocation(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long ToBundleVersion(DateTime? publishedAt) => publishedAt is null
        ? 0
        : new DateTimeOffset(DateTime.SpecifyKind(publishedAt.Value, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{1,199}$", RegexOptions.CultureInvariant)]
    private static partial Regex ContentKeyRegex();
}
