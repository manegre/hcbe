using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class OpportunityService(ApplicationDbContext db, INotificationService notices, IFileStorageService files) : IOpportunityService
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase) { "Volunteer", "Job", "Business", "Training", "Community" };
    private static readonly HashSet<string> Statuses = new(StringComparer.OrdinalIgnoreCase) { "Draft", "Published", "Closed" };

    public async Task<ApiResponse<List<OpportunityDto>>> GetPublishedAsync(string? type)
    {
        var items = await Published(type).OrderBy(x => x.DeadlineUtc).ThenByDescending(x => x.CreatedAt).ToListAsync();
        return ApiResponse<List<OpportunityDto>>.SuccessResponse(items.Select(Map).ToList());
    }

    public async Task<ApiResponse<List<OpportunityMatchDto>>> GetMatchedAsync(Guid userId, string? type)
    {
        var member = await MemberAsync(userId);
        if (member is null) return ApiResponse<List<OpportunityMatchDto>>.ErrorResponse("Member account not found");
        var result = (await Published(type).ToListAsync()).Select(x =>
        {
            var match = Match(x, member);
            return new OpportunityMatchDto(Map(x), match.Score, match.Reasons);
        }).OrderByDescending(x => x.Score).ThenBy(x => x.Opportunity.DeadlineUtc).ToList();
        return ApiResponse<List<OpportunityMatchDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<OpportunityDto>>> GetForAdminAsync() =>
        ApiResponse<List<OpportunityDto>>.SuccessResponse((await Opportunities().OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(Map).ToList());

    public async Task<ApiResponse<OpportunityDto>> CreateAsync(Guid userId, UpsertOpportunityRequest request)
    {
        var error = Validate(request); if (error is not null) return ApiResponse<OpportunityDto>.ErrorResponse(error);
        var item = new Opportunity { CreatedByUserId = userId }; Apply(item, request); db.Opportunities.Add(item);
        await db.SaveChangesAsync(); return ApiResponse<OpportunityDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, UpsertOpportunityRequest request)
    {
        var error = Validate(request); if (error is not null) return ApiResponse<OpportunityDto>.ErrorResponse(error);
        var item = await db.Opportunities.Include(x => x.Applications).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return ApiResponse<OpportunityDto>.ErrorResponse("Opportunity not found");
        Apply(item, request); await db.SaveChangesAsync(); return ApiResponse<OpportunityDto>.SuccessResponse(Map(item));
    }

    public async Task<ApiResponse> DeleteAsync(Guid id)
    {
        var item = await db.Opportunities.FindAsync(id); if (item is null) return ApiResponse.CreateError("Opportunity not found");
        item.Status = "Closed"; item.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return ApiResponse.CreateSuccess("Opportunity closed");
    }

    public async Task<ApiResponse<OpportunityApplicationDto>> ApplyAsync(Guid userId, Guid id, CreateOpportunityApplicationRequest request)
    {
        var member = await MemberAsync(userId); if (member is null) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Member account not found");
        var opportunity = await db.Opportunities.FirstOrDefaultAsync(x => x.Id == id && x.Status == "Published");
        if (opportunity is null || opportunity.DeadlineUtc < DateTime.UtcNow) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Opportunity is no longer accepting applications");
        var existing = await Applications().FirstOrDefaultAsync(x => x.OpportunityId == id && x.MemberId == member.Id);
        if (existing is not null) return ApiResponse<OpportunityApplicationDto>.SuccessResponse(MapApplication(existing));
        var match = Match(opportunity, member);
        var item = new OpportunityApplication
        {
            OpportunityId = id, MemberId = member.Id, Message = request.Message.Trim(), Experience = Clean(request.Experience),
            Availability = Clean(request.Availability), MatchScore = match.Score, MatchReasons = JsonSerializer.Serialize(match.Reasons)
        };
        db.OpportunityApplications.Add(item); await db.SaveChangesAsync();
        await notices.CreateNotificationAsync("opportunity", "Nouvelle candidature", opportunity.Title, item.Id, "/admin/opportunities");
        var created = await Applications().SingleAsync(x => x.Id == item.Id);
        return ApiResponse<OpportunityApplicationDto>.SuccessResponse(MapApplication(created));
    }

    public async Task<ApiResponse<OpportunityApplicationDocumentDto>> AddApplicationDocumentAsync(Guid userId, Guid applicationId, IFormFile file)
    {
        var memberId = await MemberIdAsync(userId);
        if (!await db.OpportunityApplications.AnyAsync(x => x.Id == applicationId && x.MemberId == memberId)) return ApiResponse<OpportunityApplicationDocumentDto>.ErrorResponse("Application not found");
        if (!files.IsAllowedExtension(file.FileName)) return ApiResponse<OpportunityApplicationDocumentDto>.ErrorResponse("Unsupported document type");
        if (file.Length > files.MaxFileSizeBytes) return ApiResponse<OpportunityApplicationDocumentDto>.ErrorResponse("Document exceeds the maximum file size");
        (string relativeUrl, string storedFileName) saved;
        try { saved = await files.SaveAsync(file, $"opportunity-applications-{applicationId:N}"); }
        catch (InvalidOperationException exception) { return ApiResponse<OpportunityApplicationDocumentDto>.ErrorResponse(exception.Message); }
        var item = new OpportunityApplicationDocument { OpportunityApplicationId = applicationId, FileName = Path.GetFileName(file.FileName), Url = saved.relativeUrl, ContentType = file.ContentType, SizeBytes = file.Length };
        db.OpportunityApplicationDocuments.Add(item); await db.SaveChangesAsync(); return ApiResponse<OpportunityApplicationDocumentDto>.SuccessResponse(MapDocument(item));
    }

    public async Task<ApiResponse> DeleteApplicationDocumentAsync(Guid userId, Guid applicationId, Guid documentId)
    {
        var memberId = await MemberIdAsync(userId);
        var item = await db.OpportunityApplicationDocuments.Include(x => x.OpportunityApplication)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.OpportunityApplicationId == applicationId && x.OpportunityApplication!.MemberId == memberId);
        if (item is null) return ApiResponse.CreateError("Document not found");
        await files.DeleteAsync(item.Url); db.OpportunityApplicationDocuments.Remove(item); await db.SaveChangesAsync(); return ApiResponse.CreateSuccess("Document deleted");
    }

    public async Task<ApiResponse<OpportunityDocumentContent>> GetApplicationDocumentAsync(Guid userId, Guid applicationId, Guid documentId, bool isAdmin)
    {
        Guid? memberId = isAdmin ? null : await MemberIdAsync(userId);
        var item = await db.OpportunityApplicationDocuments.AsNoTracking().Include(x => x.OpportunityApplication)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.OpportunityApplicationId == applicationId && (isAdmin || x.OpportunityApplication!.MemberId == memberId));
        if (item is null) return ApiResponse<OpportunityDocumentContent>.ErrorResponse("Document not found");
        var content = await files.ReadAsync(item.Url);
        return content is null
            ? ApiResponse<OpportunityDocumentContent>.ErrorResponse("Document file not found")
            : ApiResponse<OpportunityDocumentContent>.SuccessResponse(new(content.Bytes, content.ContentType, item.FileName));
    }

    public async Task<ApiResponse<VolunteerTimeEntryDto>> AddVolunteerTimeAsync(Guid userId, Guid applicationId, CreateVolunteerTimeEntryRequest request)
    {
        var memberId = await MemberIdAsync(userId);
        var application = await db.OpportunityApplications.Include(x => x.Opportunity).FirstOrDefaultAsync(x => x.Id == applicationId && x.MemberId == memberId);
        if (application is null) return ApiResponse<VolunteerTimeEntryDto>.ErrorResponse("Application not found");
        if (application.Status != "Accepted" || application.Opportunity?.Type != "Volunteer") return ApiResponse<VolunteerTimeEntryDto>.ErrorResponse("Volunteer hours can only be recorded for an accepted volunteer application");
        if (request.ActivityDate.Date > DateTime.UtcNow.Date) return ApiResponse<VolunteerTimeEntryDto>.ErrorResponse("Activity date cannot be in the future");
        var item = new VolunteerTimeEntry { OpportunityApplicationId = applicationId, ActivityDate = request.ActivityDate.Date, Hours = request.Hours, Description = request.Description.Trim() };
        db.VolunteerTimeEntries.Add(item); await db.SaveChangesAsync();
        await notices.CreateNotificationAsync("volunteer-hours", "Heures à valider", application.Opportunity.Title, item.Id, "/admin/opportunities");
        return ApiResponse<VolunteerTimeEntryDto>.SuccessResponse(MapTime(item));
    }

    public async Task<ApiResponse<VolunteerTimeEntryDto>> ReviewVolunteerTimeAsync(Guid userId, Guid id, ReviewVolunteerTimeEntryRequest request)
    {
        var status = request.Status.Trim(); if (status is not ("Approved" or "Rejected")) return ApiResponse<VolunteerTimeEntryDto>.ErrorResponse("Unsupported time entry status");
        var item = await db.VolunteerTimeEntries.Include(x => x.OpportunityApplication).ThenInclude(x => x!.Opportunity).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return ApiResponse<VolunteerTimeEntryDto>.ErrorResponse("Time entry not found");
        item.Status = status; item.ReviewNotes = Clean(request.ReviewNotes); item.ReviewedByUserId = userId; item.ReviewedAt = DateTime.UtcNow; item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); await NotifyMember(item.OpportunityApplication!.MemberId, "volunteer-hours", status == "Approved" ? "Heures approuvées" : "Heures refusées", item.OpportunityApplication.Opportunity?.Title ?? "Bénévolat", item.Id);
        return ApiResponse<VolunteerTimeEntryDto>.SuccessResponse(MapTime(item));
    }

    public async Task<ApiResponse<OpportunityCertificateDto>> IssueCertificateAsync(Guid userId, Guid applicationId, IssueOpportunityCertificateRequest request)
    {
        var item = await Applications(true).FirstOrDefaultAsync(x => x.Id == applicationId);
        if (item is null) return ApiResponse<OpportunityCertificateDto>.ErrorResponse("Application not found");
        if (item.Status != "Accepted") return ApiResponse<OpportunityCertificateDto>.ErrorResponse("Only accepted participation can be certified");
        var hours = item.VolunteerTimeEntries.Where(x => x.Status == "Approved").Sum(x => x.Hours);
        if (item.Opportunity?.Type == "Volunteer" && hours <= 0) return ApiResponse<OpportunityCertificateDto>.ErrorResponse("Approve at least one volunteer time entry before issuing an attestation");
        var certificate = item.Certificate ?? new OpportunityCertificate { OpportunityApplicationId = item.Id, CertificateNumber = CertificateNumber(), IssuedByUserId = userId };
        certificate.ContributionSummary = Clean(request.ContributionSummary); certificate.ConfirmedHours = item.Opportunity?.Type == "Volunteer" ? hours : null; certificate.IssuedByUserId = userId; certificate.IssuedAtUtc = DateTime.UtcNow;
        if (item.Certificate is null) db.OpportunityCertificates.Add(certificate); await db.SaveChangesAsync(); item.Certificate = certificate;
        await NotifyMember(item.MemberId, "attestation", "Votre attestation est prête", item.Opportunity?.Title ?? "Participation HCBE", certificate.Id);
        return ApiResponse<OpportunityCertificateDto>.SuccessResponse(MapCertificate(certificate, item.Id));
    }

    public async Task<ApiResponse<byte[]>> GetCertificatePdfAsync(Guid userId, Guid applicationId, bool isAdmin)
    {
        Guid? memberId = isAdmin ? null : await MemberIdAsync(userId);
        var item = await Applications().FirstOrDefaultAsync(x => x.Id == applicationId && (isAdmin || x.MemberId == memberId));
        return item?.Certificate is null ? ApiResponse<byte[]>.ErrorResponse("Certificate not found") : ApiResponse<byte[]>.SuccessResponse(OpportunityCertificatePdfRenderer.Render(item));
    }

    public async Task<ApiResponse<List<OpportunityApplicationDto>>> GetMineAsync(Guid userId)
    {
        var memberId = await MemberIdAsync(userId); var items = memberId is null ? [] : await Applications().Where(x => x.MemberId == memberId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return ApiResponse<List<OpportunityApplicationDto>>.SuccessResponse(items.Select(MapApplication).ToList());
    }

    public async Task<ApiResponse<List<OpportunityApplicationDto>>> GetApplicationsAsync(Guid? opportunityId)
    {
        var query = Applications(); if (opportunityId.HasValue) query = query.Where(x => x.OpportunityId == opportunityId);
        var items = await query.OrderBy(x => x.Status == "Submitted" ? 0 : 1).ThenByDescending(x => x.MatchScore).ThenByDescending(x => x.CreatedAt).ToListAsync();
        return ApiResponse<List<OpportunityApplicationDto>>.SuccessResponse(items.Select(MapApplication).ToList());
    }

    public async Task<ApiResponse<OpportunityApplicationDto>> ReviewApplicationAsync(Guid id, ReviewOpportunityApplicationRequest request)
    {
        var status = request.Status.Trim(); if (status is not ("Reviewed" or "Accepted" or "Declined")) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Unsupported application status");
        var item = await Applications(true).FirstOrDefaultAsync(x => x.Id == id); if (item is null) return ApiResponse<OpportunityApplicationDto>.ErrorResponse("Application not found");
        item.Status = status; item.AdminNotes = Clean(request.AdminNotes); item.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        await NotifyMember(item.MemberId, "opportunity", status == "Accepted" ? "Candidature acceptée" : "Mise à jour de votre candidature", item.Opportunity?.Title ?? "Occasion HCBE", item.Id);
        return ApiResponse<OpportunityApplicationDto>.SuccessResponse(MapApplication(item));
    }

    private IQueryable<Opportunity> Published(string? type) { var query = Opportunities().Where(x => x.Status == "Published" && (x.DeadlineUtc == null || x.DeadlineUtc >= DateTime.UtcNow)); return string.IsNullOrWhiteSpace(type) ? query : query.Where(x => x.Type == type); }
    private IQueryable<Opportunity> Opportunities() => db.Opportunities.AsNoTracking().Include(x => x.Applications);
    private IQueryable<OpportunityApplication> Applications(bool tracking = false) { var query = db.OpportunityApplications.Include(x => x.Opportunity).Include(x => x.Member).Include(x => x.Documents).Include(x => x.VolunteerTimeEntries).Include(x => x.Certificate).AsSplitQuery(); return tracking ? query : query.AsNoTracking(); }
    private Task<Guid?> MemberIdAsync(Guid userId) => db.Users.AsNoTracking().Where(x => x.Id == userId && x.IsActive).Select(x => x.MemberId).SingleOrDefaultAsync();
    private Task<Member?> MemberAsync(Guid userId) => db.Users.AsNoTracking().Where(x => x.Id == userId && x.IsActive).Select(x => x.Member).SingleOrDefaultAsync();
    private async Task NotifyMember(Guid memberId, string type, string title, string message, Guid entityId) { var userId = await db.Users.AsNoTracking().Where(x => x.MemberId == memberId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(); if (userId.HasValue) await notices.CreateForUserAsync(userId.Value, type, title, message, entityId, "/espace-membre?section=opportunities"); }

    private static string? Validate(UpsertOpportunityRequest r) { if (!Types.Contains(r.Type)) return "Unsupported opportunity type"; if (!Statuses.Contains(r.Status)) return "Unsupported opportunity status"; if (r.StartsAtUtc.HasValue && r.EndsAtUtc <= r.StartsAtUtc) return "End date must be after start date"; if (r.StartsAtUtc.HasValue && r.DeadlineUtc > r.StartsAtUtc) return "Application deadline must be before the start date"; return null; }
    private static void Apply(Opportunity x, UpsertOpportunityRequest r) { x.Title=r.Title.Trim(); x.TitleEn=Clean(r.TitleEn); x.Description=r.Description.Trim(); x.DescriptionEn=Clean(r.DescriptionEn); x.Type=Types.First(v=>v.Equals(r.Type,StringComparison.OrdinalIgnoreCase)); x.Organization=r.Organization.Trim(); x.Location=Clean(r.Location); x.Region=Clean(r.Region); x.IsRemote=r.IsRemote; x.Skills=Clean(r.Skills); x.Availability=Clean(r.Availability); x.Commitment=Clean(r.Commitment); x.Requirements=Clean(r.Requirements); x.RequirementsEn=Clean(r.RequirementsEn); x.Benefits=Clean(r.Benefits); x.BenefitsEn=Clean(r.BenefitsEn); x.ContactEmail=Clean(r.ContactEmail); x.ApplyUrl=Clean(r.ApplyUrl); x.StartsAtUtc=Utc(r.StartsAtUtc); x.EndsAtUtc=Utc(r.EndsAtUtc); x.DeadlineUtc=Utc(r.DeadlineUtc); x.Status=Statuses.First(v=>v.Equals(r.Status,StringComparison.OrdinalIgnoreCase)); x.UpdatedAt=DateTime.UtcNow; }
    private static OpportunityDto Map(Opportunity x) => new(x.Id,x.Title,x.TitleEn,x.Description,x.DescriptionEn,x.Type,x.Organization,x.Location,x.IsRemote,x.Skills,x.ApplyUrl,x.DeadlineUtc,x.Status,x.Applications.Count,x.CreatedAt,x.UpdatedAt,x.Region,x.Availability,x.Commitment,x.Requirements,x.RequirementsEn,x.Benefits,x.BenefitsEn,x.ContactEmail,x.StartsAtUtc,x.EndsAtUtc);
    private static OpportunityApplicationDto MapApplication(OpportunityApplication x) => new(x.Id,x.OpportunityId,x.Opportunity?.Title??"",x.Opportunity?.TitleEn,x.MemberId,$"{x.Member?.FirstName} {x.Member?.LastName}".Trim(),x.Member?.Email??"",x.Message,x.Status,x.AdminNotes,x.CreatedAt,x.UpdatedAt,x.Experience,x.Availability,x.MatchScore,ParseReasons(x.MatchReasons),x.Documents.OrderBy(d=>d.CreatedAt).Select(MapDocument).ToList(),x.VolunteerTimeEntries.OrderByDescending(t=>t.ActivityDate).Select(MapTime).ToList(),x.Certificate is null?null:MapCertificate(x.Certificate,x.Id),x.VolunteerTimeEntries.Where(t=>t.Status=="Approved").Sum(t=>t.Hours),x.Opportunity?.Type??"Community");
    private static OpportunityApplicationDocumentDto MapDocument(OpportunityApplicationDocument x) => new(x.Id,x.FileName,$"/api/opportunities/applications/{x.OpportunityApplicationId}/documents/{x.Id}/download",x.ContentType,x.SizeBytes,x.CreatedAt);
    private static VolunteerTimeEntryDto MapTime(VolunteerTimeEntry x) => new(x.Id,x.ActivityDate,x.Hours,x.Description,x.Status,x.ReviewNotes,x.ReviewedAt,x.CreatedAt,x.UpdatedAt);
    private static OpportunityCertificateDto MapCertificate(OpportunityCertificate x, Guid applicationId) => new(x.Id,x.CertificateNumber,x.ContributionSummary,x.ConfirmedHours,x.IssuedAtUtc,$"/api/opportunities/applications/{applicationId}/certificate");
    private static (int Score,List<string> Reasons) Match(Opportunity o, Member m) { var score=20; var reasons=new List<string>(); if (Tokens(m.Expertise,m.Profession,m.Interests).Intersect(Tokens(o.Skills,o.Requirements)).Any()){score+=45;reasons.Add("skills");} if(o.IsRemote){score+=15;reasons.Add("remote");}else if(Contains(o.Region,m.Province,m.Zone)||Contains(o.Location,m.City,m.Province)){score+=20;reasons.Add("region");} if(Contains(o.Availability,m.Availability)){score+=20;reasons.Add("availability");} return(Math.Min(score,100),reasons); }
    private static HashSet<string> Tokens(params string?[] values) => values.Where(x=>!string.IsNullOrWhiteSpace(x)).SelectMany(x=>x!.Split([',',';','|','/',' '],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)).Where(x=>x.Length>2).Select(x=>x.ToLowerInvariant()).ToHashSet();
    private static bool Contains(string? source, params string?[] candidates) => !string.IsNullOrWhiteSpace(source)&&candidates.Any(x=>!string.IsNullOrWhiteSpace(x)&&source.Contains(x,StringComparison.OrdinalIgnoreCase));
    private static IReadOnlyList<string> ParseReasons(string? value) { try { return string.IsNullOrWhiteSpace(value)?[]:JsonSerializer.Deserialize<List<string>>(value)??[]; } catch(JsonException){ return []; } }
    private static string CertificateNumber() => $"HCBE-ATT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    private static DateTime? Utc(DateTime? value) => value?.ToUniversalTime();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
