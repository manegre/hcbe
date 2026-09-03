namespace HcbeApi.Models;

public sealed class AssociationClaimRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssociationId { get; set; }
    public Association? Association { get; set; }
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
