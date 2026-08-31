namespace HcbeApi.Models;

public class News
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string? Excerpt { get; set; }
    public string? ExcerptEn { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImagePosition { get; set; } = "center";
    public string? Author { get; set; }
    public string? Category { get; set; }
    public DateTime? PublishedDate { get; set; }
    public bool IsPinned { get; set; } = false;
    public string Status { get; set; } = "published";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<NewsAttachment> Attachments { get; set; } = new List<NewsAttachment>();
}

