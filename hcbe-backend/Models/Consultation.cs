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
    /// <summary>Information | Survey | Proposal | Vote</summary>
    public string GovernanceType { get; set; } = "Information";
    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }
    public DateTime? CommentClosesAtUtc { get; set; }
    /// <summary>Named | Anonymous</summary>
    public string VotingMode { get; set; } = "Named";
    /// <summary>AllMembers | ActiveMembers | Administrators</summary>
    public string EligibilityRule { get; set; } = "ActiveMembers";
    public int QuorumPercentage { get; set; }
    public int MinimumParticipation { get; set; }
    public bool AllowComments { get; set; }
    public DateTime? ResultsPublishedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ConsultationOption> Options { get; set; } = new List<ConsultationOption>();
    public ICollection<ConsultationComment> Comments { get; set; } = new List<ConsultationComment>();
    public ICollection<ConsultationParticipation> Participations { get; set; } = new List<ConsultationParticipation>();
    public ICollection<ConsultationBallot> Ballots { get; set; } = new List<ConsultationBallot>();
    public ICollection<ConsultationAuditEvent> AuditEvents { get; set; } = new List<ConsultationAuditEvent>();
}

public sealed class ConsultationOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public string? LabelEn { get; set; }
    public int DisplayOrder { get; set; }
    public ICollection<ConsultationBallot> Ballots { get; set; } = new List<ConsultationBallot>();
}

public sealed class ConsultationComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ConsultationParticipation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTime ParticipatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ConsultationBallot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public Guid OptionId { get; set; }
    public ConsultationOption Option { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CastAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ConsultationAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
