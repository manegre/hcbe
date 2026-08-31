namespace HcbeApi.Models;

public class ServiceContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public string? CategoryEn { get; set; }
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
    public string? Details { get; set; } // JSON string
    public string? DetailsEn { get; set; }
    public string? ExtendedInfo { get; set; } // JSON string
    public string? ExtendedInfoEn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

