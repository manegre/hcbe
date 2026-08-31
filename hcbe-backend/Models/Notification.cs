namespace HcbeApi.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty; // member, event, news, project, document
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; } // ID of the related entity (member, event, etc.)
    public string? Link { get; set; } // Link to navigate to the related entity
    public bool IsRead { get; set; } = false;
    public Guid? UserId { get; set; } // If null, notification is for all admins
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

