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
    public string OrganizationType { get; set; } = "Association";
    public bool IsActive { get; set; } = true;
    public Guid? OwnerMemberId { get; set; }
    public Member? OwnerMember { get; set; }
    public ICollection<AssociationMember> Members { get; set; } = new List<AssociationMember>();
    public ICollection<AssociationJoinRequest> JoinRequests { get; set; } = new List<AssociationJoinRequest>();
    public ICollection<AssociationDocument> Documents { get; set; } = new List<AssociationDocument>();
    public ICollection<AssociationCalendarItem> CalendarItems { get; set; } = new List<AssociationCalendarItem>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
