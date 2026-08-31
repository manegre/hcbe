namespace HcbeApi.Models;

public class PrivateConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberOneId { get; set; }
    public Member? MemberOne { get; set; }
    public Guid MemberTwoId { get; set; }
    public Member? MemberTwo { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public Guid RelationshipId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
}

public class PrivateMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public PrivateConversation? Conversation { get; set; }
    public Guid SenderMemberId { get; set; }
    public Member? SenderMember { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public class ConversationReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public PrivateConversation? Conversation { get; set; }
    public Guid ReporterMemberId { get; set; }
    public Member? ReporterMember { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
