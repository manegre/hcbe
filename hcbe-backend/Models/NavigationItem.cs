namespace HcbeApi.Models;

public class NavigationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string? LabelEn { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

