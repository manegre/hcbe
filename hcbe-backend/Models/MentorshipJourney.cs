namespace HcbeApi.Models;
public sealed class MentorshipGoal
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid MatchId { get; set; } public MentorshipMatch? Match { get; set; }
    public Guid CreatedByMemberId { get; set; } public string Title { get; set; } = string.Empty; public string Status { get; set; } = "Open";
    public DateTime? DueAtUtc { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class MentorshipCheckIn
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid MatchId { get; set; } public MentorshipMatch? Match { get; set; }
    public Guid MemberId { get; set; } public Member? Member { get; set; } public string Summary { get; set; } = string.Empty;
    public int Rating { get; set; } public bool NeedsCommitteeSupport { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
