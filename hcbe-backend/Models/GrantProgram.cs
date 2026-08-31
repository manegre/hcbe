namespace HcbeApi.Models;

public class GrantProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string Icon { get; set; } = "ri-graduation-cap-line";
    public string Amount { get; set; } = string.Empty;
    public string? AmountEn { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string? DurationEn { get; set; }
    public List<string> EligibilityCriteria { get; set; } = new();
    public List<string> EligibilityCriteriaEn { get; set; } = new();
    public string? ApplicationUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
