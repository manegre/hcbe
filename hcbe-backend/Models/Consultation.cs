namespace HcbeApi.Models;

public class Consultation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string Icon { get; set; } = "ri-chat-poll-line";
    /// <summary>featured = large banner, card = grid item</summary>
    public string LayoutType { get; set; } = "card";
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
    public string? ActionLabelEn { get; set; }
    public string? SecondaryActionUrl { get; set; }
    public string? SecondaryActionLabel { get; set; }
    public string? SecondaryActionLabelEn { get; set; }
    /// <summary>emerald | amber — accent for card layout</summary>
    public string AccentColor { get; set; } = "emerald";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
