namespace HcbeApi.Models;

public class EventMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    /// <summary>image | video</summary>
    public string MediaType { get; set; } = "image";
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Caption { get; set; }
    public string? CaptionEn { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
