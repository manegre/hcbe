namespace HcbeApi.Models;

public class Association
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? President { get; set; }
    public string? MemberCount { get; set; }
    public int? FoundedYear { get; set; }
    public string? ImageUrl { get; set; }
    public string? Website { get; set; }
    public List<string> Domains { get; set; } = new();
    public List<string> DomainsEn { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public Guid? OwnerMemberId { get; set; }
    public Member? OwnerMember { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
