using System.Security.Cryptography;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class ServiceCaseService(
    ApplicationDbContext context,
    INotificationService notifications,
    IFileStorageService fileStorage,
    IEmailOutbox emailOutbox,
    IEmailTemplateRenderer emailTemplates,
    IConfiguration configuration) : IServiceCaseService
{
    private static readonly HashSet<string> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        "integration", "employment", "legal", "education", "business", "social-support", "culture", "other"
    };
    private static readonly HashSet<string> Statuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Submitted", "InReview", "AwaitingMember", "Resolved", "Closed"
    };
    private static readonly HashSet<string> Priorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Normal", "High", "Urgent"
    };

    public async Task<ApiResponse<ServiceCaseDto>> CreateAsync(Guid userId, CreateServiceCaseRequest request)
    {
        var member = await MemberForUserAsync(userId);
        if (member is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("A member account is required");
        var category = Categories.FirstOrDefault(item => item.Equals(request.Category?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (category is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Unsupported service category");

        var item = new ServiceCase
        {
            TicketNumber = await NextTicketNumberAsync(),
            MemberId = member.Id,
            Member = member,
            Category = category,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim()
        };
        context.ServiceCases.Add(item);
        await context.SaveChangesAsync();

        await notifications.CreateNotificationAsync("service-case", "Nouvelle demande de service", $"{item.TicketNumber} — {item.Subject}", item.Id, $"/admin/service-cases/{item.Id}");
        QueueEmail(item, member, "Submitted", null);
        await context.SaveChangesAsync();
        return ApiResponse<ServiceCaseDto>.SuccessResponse(Map(item, includeInternal: false));
    }

    public async Task<ApiResponse<List<ServiceCaseDto>>> GetMineAsync(Guid userId)
    {
        var memberId = await MemberIdForUserAsync(userId);
        if (memberId is null) return ApiResponse<List<ServiceCaseDto>>.ErrorResponse("Member account not found");
        var items = await Query().Where(item => item.MemberId == memberId).OrderByDescending(item => item.UpdatedAt).ToListAsync();
        return ApiResponse<List<ServiceCaseDto>>.SuccessResponse(items.Select(item => Map(item, false)).ToList());
    }

    public async Task<ApiResponse<ServiceCaseDto>> GetMineByIdAsync(Guid userId, Guid id)
    {
        var memberId = await MemberIdForUserAsync(userId);
        var item = memberId is null ? null : await Query().FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.MemberId == memberId);
        return item is null ? ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found") : ApiResponse<ServiceCaseDto>.SuccessResponse(Map(item, false));
    }

    public async Task<ApiResponse<ServiceCaseDto>> AddMemberMessageAsync(Guid userId, Guid id, AddServiceCaseMessageRequest request)
    {
        var memberId = await MemberIdForUserAsync(userId);
        var item = memberId is null ? null : await Query(true).FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.MemberId == memberId);
        if (item is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found");
        if (item.Status == "Closed") return ApiResponse<ServiceCaseDto>.ErrorResponse("This service request is closed");

        var message = new ServiceCaseMessage
        {
            ServiceCaseId = item.Id,
            AuthorUserId = userId,
            Body = request.Body.Trim(),
            IsInternal = false
        };
        context.ServiceCaseMessages.Add(message);
        item.Status = item.Status == "AwaitingMember" ? "InReview" : item.Status;
        item.LastResponseAt = item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await notifications.CreateNotificationAsync("service-case", "Réponse d’un membre", $"{item.TicketNumber} — {item.Subject}", item.Id, $"/admin/service-cases/{item.Id}");
        return ApiResponse<ServiceCaseDto>.SuccessResponse(Map(item, false));
    }

    public Task<ApiResponse<ServiceCaseAttachmentDto>> AddMemberAttachmentAsync(Guid userId, Guid id, IFormFile file) =>
        AddAttachmentAsync(userId, id, file, false, requireOwnership: true);

    public async Task<ApiResponse<List<ServiceCaseDto>>> GetForAdminAsync(string? status, string? category, string? search)
    {
        var query = Query();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.Category == category.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(item => item.TicketNumber.ToLower().Contains(term) || item.Subject.ToLower().Contains(term) || item.Member!.Email.ToLower().Contains(term) || (item.Member.FirstName + " " + item.Member.LastName).ToLower().Contains(term));
        }
        var items = await query.OrderBy(item => item.Status == "Submitted" ? 0 : 1).ThenByDescending(item => item.UpdatedAt).ToListAsync();
        return ApiResponse<List<ServiceCaseDto>>.SuccessResponse(items.Select(item => Map(item, true)).ToList());
    }

    public async Task<ApiResponse<ServiceCaseDto>> GetForAdminByIdAsync(Guid id)
    {
        var item = await Query().FirstOrDefaultAsync(candidate => candidate.Id == id);
        return item is null ? ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found") : ApiResponse<ServiceCaseDto>.SuccessResponse(Map(item, true));
    }

    public async Task<ApiResponse<ServiceCaseDto>> UpdateForAdminAsync(Guid id, UpdateServiceCaseRequest request)
    {
        var item = await Query(true).FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found");
        var previousStatus = item.Status;

        if (request.Status is not null)
        {
            var status = Statuses.FirstOrDefault(candidate => candidate.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
            if (status is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Unsupported service request status");
            item.Status = status;
            item.ResolvedAt = status is "Resolved" or "Closed" ? item.ResolvedAt ?? DateTime.UtcNow : null;
        }
        if (request.Priority is not null)
        {
            var priority = Priorities.FirstOrDefault(candidate => candidate.Equals(request.Priority.Trim(), StringComparison.OrdinalIgnoreCase));
            if (priority is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Unsupported priority");
            item.Priority = priority;
        }
        if (request.ClearAssignee) item.AssignedToUserId = null;
        else if (request.AssignedToUserId.HasValue)
        {
            var validAdmin = await context.Users.AnyAsync(user => user.Id == request.AssignedToUserId && user.IsAdmin && user.IsActive);
            if (!validAdmin) return ApiResponse<ServiceCaseDto>.ErrorResponse("Assignee must be an active administrator");
            item.AssignedToUserId = request.AssignedToUserId;
        }
        if (request.ClearAssociation) { item.AssignedAssociationId = null; item.AssignedAssociation = null; }
        else if (request.AssignedAssociationId.HasValue)
        {
            var validOrganization = await context.Associations.FirstOrDefaultAsync(association => association.Id == request.AssignedAssociationId && association.IsActive);
            if (validOrganization is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Assigned organization must be active");
            item.AssignedAssociation = validOrganization;
            if (item.Status == "Submitted") item.Status = "InReview";
        }
        if (request.InternalNotes is not null) item.InternalNotes = Normalize(request.InternalNotes);
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        if (previousStatus != item.Status)
        {
            QueueEmail(item, item.Member!, item.Status, null);
            await context.SaveChangesAsync();
        }
        return ApiResponse<ServiceCaseDto>.SuccessResponse(Map(item, true));
    }

    public async Task<ApiResponse<ServiceCaseDto>> AddAdminMessageAsync(Guid userId, Guid id, AddServiceCaseMessageRequest request)
    {
        var item = await Query(true).FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null) return ApiResponse<ServiceCaseDto>.ErrorResponse("Service request not found");
        var message = new ServiceCaseMessage
        {
            ServiceCaseId = item.Id,
            AuthorUserId = userId,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal
        };
        context.ServiceCaseMessages.Add(message);
        item.LastResponseAt = item.UpdatedAt = DateTime.UtcNow;
        if (!request.IsInternal && item.Status is "Submitted" or "InReview") item.Status = "AwaitingMember";
        await context.SaveChangesAsync();
        if (!request.IsInternal)
        {
            QueueEmail(item, item.Member!, item.Status, message.Body);
            await context.SaveChangesAsync();
        }
        return ApiResponse<ServiceCaseDto>.SuccessResponse(Map(item, true));
    }

    public Task<ApiResponse<ServiceCaseAttachmentDto>> AddAdminAttachmentAsync(Guid userId, Guid id, IFormFile file, bool isInternal) =>
        AddAttachmentAsync(userId, id, file, isInternal, requireOwnership: false);

    private async Task<ApiResponse<ServiceCaseAttachmentDto>> AddAttachmentAsync(Guid userId, Guid id, IFormFile file, bool isInternal, bool requireOwnership)
    {
        var memberId = requireOwnership ? await MemberIdForUserAsync(userId) : null;
        var item = await context.ServiceCases.FirstOrDefaultAsync(candidate => candidate.Id == id && (!requireOwnership || candidate.MemberId == memberId));
        if (item is null) return ApiResponse<ServiceCaseAttachmentDto>.ErrorResponse("Service request not found");
        if (!fileStorage.IsAllowedExtension(file.FileName)) return ApiResponse<ServiceCaseAttachmentDto>.ErrorResponse("File type not allowed");
        var saved = await fileStorage.SaveAsync(file, $"service-cases/{item.Id:N}");
        var attachment = new ServiceCaseAttachment
        {
            ServiceCaseId = item.Id,
            UploadedByUserId = userId,
            FileName = Path.GetFileName(file.FileName),
            Url = saved.relativeUrl,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            IsInternal = isInternal
        };
        context.ServiceCaseAttachments.Add(attachment);
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return ApiResponse<ServiceCaseAttachmentDto>.SuccessResponse(MapAttachment(attachment));
    }

    private IQueryable<ServiceCase> Query(bool tracking = false)
    {
        var query = context.ServiceCases
            .Include(item => item.Member)
            .Include(item => item.AssignedToUser)
            .Include(item => item.AssignedAssociation)
            .Include(item => item.Messages).ThenInclude(message => message.AuthorUser)
            .Include(item => item.Attachments)
            .AsSplitQuery();
        return tracking ? query : query.AsNoTracking();
    }

    private ServiceCaseDto Map(ServiceCase item, bool includeInternal) => new(
        item.Id, item.TicketNumber, item.MemberId,
        $"{item.Member?.FirstName} {item.Member?.LastName}".Trim(), item.Member?.Email ?? string.Empty,
        item.Category, item.Subject, item.Description, item.Status, item.Priority,
        item.AssignedToUserId, item.AssignedToUser is null ? null : $"{item.AssignedToUser.FirstName} {item.AssignedToUser.LastName}".Trim(),
        includeInternal ? item.InternalNotes : null, item.AssignedAssociationId, item.AssignedAssociation?.Name,
        item.CreatedAt, item.UpdatedAt, item.LastResponseAt, item.ResolvedAt,
        item.Messages.Where(message => includeInternal || !message.IsInternal).OrderBy(message => message.CreatedAt)
            .Select(message => new ServiceCaseMessageDto(message.Id, message.AuthorUserId, $"{message.AuthorUser?.FirstName} {message.AuthorUser?.LastName}".Trim(), message.Body, message.IsInternal, message.CreatedAt)).ToList(),
        item.Attachments.Where(attachment => includeInternal || !attachment.IsInternal).OrderBy(attachment => attachment.CreatedAt).Select(MapAttachment).ToList());

    private static ServiceCaseAttachmentDto MapAttachment(ServiceCaseAttachment item) => new(item.Id, item.FileName, item.Url, item.ContentType, item.SizeBytes, item.IsInternal, item.CreatedAt);

    private async Task<string> NextTicketNumberAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
            var ticket = $"HCBE-{DateTime.UtcNow:yyyy}-{suffix}";
            if (!await context.ServiceCases.AnyAsync(item => item.TicketNumber == ticket)) return ticket;
        }
        return $"HCBE-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }

    private Task<Member?> MemberForUserAsync(Guid userId) => context.Users.Where(user => user.Id == userId && user.IsActive && user.MemberId != null).Select(user => user.Member).FirstOrDefaultAsync();
    private Task<Guid?> MemberIdForUserAsync(Guid userId) => context.Users.AsNoTracking().Where(user => user.Id == userId && user.IsActive).Select(user => user.MemberId).FirstOrDefaultAsync();
    private string PublicAppUrl() => (configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
    private void QueueEmail(ServiceCase item, Member member, string status, string? message)
    {
        var email = emailTemplates.ServiceCaseUpdate(member.FirstName, item.TicketNumber, item.Subject, status, message, $"{PublicAppUrl()}/espace-membre?section=services&case={item.Id}");
        emailOutbox.Enqueue(member.Email, email.Subject, email.HtmlBody, nameof(ServiceCase), item.Id);
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
