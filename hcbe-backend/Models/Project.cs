namespace HcbeApi.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? LocationEn { get; set; }
    public string Type { get; set; } = string.Empty; // "Développement au Burkina", "Initiative Locale"
    public string Status { get; set; } = string.Empty; // "En cours", "Actif", "Planification", "Terminé"
    public int Progress { get; set; } = 0; // 0-100
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? ImageUrl { get; set; }
    public string Budget { get; set; } = string.Empty; // e.g., "150 000 $ CAD"
    public string FundsRaised { get; set; } = string.Empty; // e.g., "97 500 $ CAD"
    public string Beneficiaries { get; set; } = string.Empty; // e.g., "300 élèves"
    public string? BeneficiariesEn { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Partners { get; set; } = new(); // JSON serialized list
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

