namespace HcbeApi.Models;

public enum MembershipApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

public class MembershipApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Profession { get; set; }
    public string? Expertise { get; set; }
    public string? Motivation { get; set; }
    public string? PasswordHash { get; set; }
    public MembershipApplicationStatus Status { get; set; } = MembershipApplicationStatus.Pending;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
