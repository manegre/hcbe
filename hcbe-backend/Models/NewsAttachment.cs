namespace HcbeApi.Models;

public class NewsAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NewsId { get; set; }
    public News News { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
