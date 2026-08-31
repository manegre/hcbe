namespace HcbeApi.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Icon { get; set; }
    public string? Type { get; set; }
    public string? Size { get; set; }
    public string? Pages { get; set; }
    public string? PagesEn { get; set; }
    public string? Category { get; set; }
    public string? CategoryEn { get; set; }
    public string? Url { get; set; }
    public int Downloads { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

