namespace HcbeApi.Models;

public sealed class SavedMemberItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MemberBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BlockerMemberId { get; set; }
    public Member? BlockerMember { get; set; }
    public Guid BlockedMemberId { get; set; }
    public Member? BlockedMember { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
