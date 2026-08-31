namespace HcbeApi.Models;

public class PageSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Page { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? TitleEn { get; set; }
    public string? Content { get; set; }
    public string? ContentEn { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

