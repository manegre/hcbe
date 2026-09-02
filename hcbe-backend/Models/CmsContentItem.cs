namespace HcbeApi.Models;

public class CmsContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Page { get; set; } = "global";
    public string Section { get; set; } = "general";
    public string ContentType { get; set; } = "text";
    public string? Label { get; set; }
    public string? DraftValueFr { get; set; }
    public string? DraftValueEn { get; set; }
    public string? PublishedValueFr { get; set; }
    public string? PublishedValueEn { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public List<CmsContentRevision> Revisions { get; set; } = [];
}

public class CmsContentRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CmsContentItemId { get; set; }
    public CmsContentItem? CmsContentItem { get; set; }
    public int Version { get; set; }
    public string? ValueFr { get; set; }
    public string? ValueEn { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
