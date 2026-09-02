using System.ComponentModel.DataAnnotations;

namespace HcbeApi.Models;

public record CmsPublishedContentDto(
    string Key,
    string ContentType,
    string? ValueFr,
    string? ValueEn,
    int Version);

public record CmsPublishedBundleDto(
    long Version,
    DateTime? PublishedAt,
    List<CmsPublishedContentDto> Items);

public record CmsContentItemDto(
    Guid Id,
    string Key,
    string Page,
    string Section,
    string ContentType,
    string? Label,
    string? DraftValueFr,
    string? DraftValueEn,
    string? PublishedValueFr,
    string? PublishedValueEn,
    bool IsPublished,
    bool HasUnpublishedChanges,
    int Version,
    DateTime UpdatedAt,
    DateTime? PublishedAt);

public record UpsertCmsContentRequest(
    [Required] string Key,
    string? Page,
    string? Section,
    string? ContentType,
    string? Label,
    string? ValueFr,
    string? ValueEn,
    bool Publish = false);

public record CmsContentRevisionDto(
    Guid Id,
    int Version,
    string? ValueFr,
    string? ValueEn,
    Guid? PublishedByUserId,
    DateTime PublishedAt);

public record CmsPublishResultDto(int PublishedCount, long Version, DateTime PublishedAt);
