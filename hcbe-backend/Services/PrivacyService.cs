using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Services;

public sealed class PrivacyService(
    ApplicationDbContext context,
    IConfiguration configuration,
    ILogger<PrivacyService> logger) : IPrivacyService
{
    public async Task<byte[]?> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user == null) return null;
        var memberId = user.MemberId;
        var conversationIds = memberId == null
            ? new List<Guid>()
            : await context.PrivateConversations.AsNoTracking()
                .Where(item => item.MemberOneId == memberId || item.MemberTwoId == memberId)
                .Select(item => item.Id).ToListAsync(cancellationToken);

        var export = new
        {
            generatedAtUtc = DateTime.UtcNow,
            account = new { user.Id, user.Email, user.FirstName, user.LastName, user.IsAdmin, user.MemberId, user.CreatedAt, user.LastLoginAtUtc },
            communicationPreferences = await context.MemberPreferences.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken),
            member = memberId == null ? null : await context.Members.AsNoTracking().SingleOrDefaultAsync(item => item.Id == memberId, cancellationToken),
            networkingProfile = memberId == null ? null : await context.NetworkingProfiles.AsNoTracking().SingleOrDefaultAsync(item => item.MemberId == memberId, cancellationToken),
            mentorshipApplications = memberId == null ? [] : await context.MentorshipApplications.AsNoTracking().Where(item => item.MemberId == memberId).ToListAsync(cancellationToken),
            mentorshipMatches = memberId == null ? [] : await context.MentorshipMatches.AsNoTracking()
                .Where(item => item.MentorApplication!.MemberId == memberId || item.MenteeApplication!.MemberId == memberId)
                .ToListAsync(cancellationToken),
            mentorshipGoals = memberId == null ? [] : await context.MentorshipGoals.AsNoTracking().Where(item => item.CreatedByMemberId == memberId).ToListAsync(cancellationToken),
            mentorshipCheckIns = memberId == null ? [] : await context.MentorshipCheckIns.AsNoTracking().Where(item => item.MemberId == memberId).ToListAsync(cancellationToken),
            associationClaims = memberId == null ? [] : await context.AssociationClaimRequests.AsNoTracking().Where(item => item.MemberId == memberId).ToListAsync(cancellationToken),
            opportunityApplications = memberId == null ? [] : await context.OpportunityApplications.AsNoTracking().Where(item => item.MemberId == memberId).ToListAsync(cancellationToken),
            connectionRequests = memberId == null ? [] : await context.ConnectionRequests.AsNoTracking().Where(item => item.RequesterMemberId == memberId || item.RecipientMemberId == memberId).ToListAsync(cancellationToken),
            conversations = await context.PrivateConversations.AsNoTracking().Where(item => conversationIds.Contains(item.Id)).ToListAsync(cancellationToken),
            messages = await context.PrivateMessages.AsNoTracking().Where(item => conversationIds.Contains(item.ConversationId)).ToListAsync(cancellationToken),
            reports = memberId == null ? [] : await context.ConversationReports.AsNoTracking().Where(item => item.ReporterMemberId == memberId).ToListAsync(cancellationToken),
            eventRegistrations = memberId == null ? [] : await context.EventRegistrations.AsNoTracking().Where(item => item.MemberId == memberId).ToListAsync(cancellationToken),
            serviceCases = memberId == null ? [] : await context.ServiceCases.AsNoTracking()
                .Where(item => item.MemberId == memberId)
                .Select(item => new
                {
                    item.Id, item.TicketNumber, item.Category, item.Subject, item.Description, item.Status,
                    item.Priority, item.CreatedAt, item.UpdatedAt, item.LastResponseAt, item.ResolvedAt,
                    messages = item.Messages.Where(message => !message.IsInternal).Select(message => new
                    {
                        message.Id, message.AuthorUserId, message.Body, message.CreatedAt
                    }),
                    attachments = item.Attachments.Where(attachment => !attachment.IsInternal).Select(attachment => new
                    {
                        attachment.Id, attachment.FileName, attachment.Url, attachment.ContentType,
                        attachment.SizeBytes, attachment.CreatedAt
                    })
                }).ToListAsync(cancellationToken),
            membershipApplications = await context.MembershipApplications.AsNoTracking().Where(item => item.Email == user.Email).Select(item => new
            {
                item.Id, item.FirstName, item.LastName, item.Email, item.Phone, item.City, item.Province,
                item.Profession, item.Expertise, item.Motivation, item.Status, item.MemberId, item.CreatedAt, item.ReviewedAt
            }).ToListAsync(cancellationToken),
            newsletterSubscriptions = await context.NewsletterSubscriptions.AsNoTracking().Where(item => item.Email == user.Email).Select(item => new
            {
                item.Id, item.Email, item.FullName, item.PreferredLanguage, item.ConsentAcceptedAt,
                item.IsActive, item.Source, item.CreatedAt, item.UpdatedAt
            }).ToListAsync(cancellationToken)
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public async Task<ApiResponse<PrivacyRequestDto>> RequestDeletionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user == null) return ApiResponse<PrivacyRequestDto>.ErrorResponse("Account not found");
        if (user.IsAdmin) return ApiResponse<PrivacyRequestDto>.ErrorResponse("Administrator access must be transferred before account deletion");

        var existing = await context.PrivacyRequests.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.Status == "Pending", cancellationToken);
        if (existing != null) return ApiResponse<PrivacyRequestDto>.SuccessResponse(Map(existing));

        var delayDays = Math.Clamp(configuration.GetValue("Privacy:DeletionDelayDays", 30), 1, 90);
        var request = new PrivacyRequest
        {
            UserId = userId,
            ExecuteAfterUtc = DateTime.UtcNow.AddDays(delayDays)
        };
        context.PrivacyRequests.Add(request);
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse<PrivacyRequestDto>.SuccessResponse(Map(request));
    }

    public async Task<ApiResponse> CancelDeletionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var request = await context.PrivacyRequests
            .FirstOrDefaultAsync(item => item.UserId == userId && item.Status == "Pending", cancellationToken);
        if (request == null) return ApiResponse.CreateError("No pending deletion request found");
        request.Status = "Cancelled";
        request.CancelledAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return ApiResponse.CreateSuccess("Deletion request cancelled");
    }

    public async Task<int> ProcessDueDeletionsAsync(CancellationToken cancellationToken)
    {
        var due = await context.PrivacyRequests
            .Where(item => item.Status == "Pending" && item.ExecuteAfterUtc <= DateTime.UtcNow)
            .OrderBy(item => item.ExecuteAfterUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var request in due)
        {
            try
            {
                await AnonymizeAsync(request, cancellationToken);
            }
            catch (Exception exception)
            {
                request.Status = "Failed";
                request.FailureReason = exception.Message.Length > 1000 ? exception.Message[..1000] : exception.Message;
                logger.LogError(exception, "Privacy deletion request {PrivacyRequestId} failed", request.Id);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        return due.Count;
    }

    public async Task<int> PurgeExpiredOperationalDataAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var passwordTokens = await context.PasswordResetTokens.Where(item => item.ExpiresAt < now.AddDays(-7)).ToListAsync(cancellationToken);
        var refreshTokens = await context.RefreshTokens.Where(item => item.ExpiresAtUtc < now.AddDays(-30) || item.RevokedAtUtc < now.AddDays(-30)).ToListAsync(cancellationToken);
        var sentOutbox = await context.EmailOutboxMessages.Where(item => item.Status == "Sent" && item.ProcessedAtUtc < now.AddDays(-90)).ToListAsync(cancellationToken);
        var deadOutbox = await context.EmailOutboxMessages.Where(item => item.Status == "DeadLetter" && item.ProcessedAtUtc < now.AddDays(-365)).ToListAsync(cancellationToken);
        var auditDays = Math.Clamp(configuration.GetValue("Privacy:AuditRetentionDays", 730), 90, 2555);
        var auditLogs = await context.AuditLogs.Where(item => item.CreatedAtUtc < now.AddDays(-auditDays)).ToListAsync(cancellationToken);
        var notifications = await context.Notifications.Where(item => item.CreatedAt < now.AddDays(-365)).ToListAsync(cancellationToken);

        context.RemoveRange(passwordTokens);
        context.RemoveRange(refreshTokens);
        context.RemoveRange(sentOutbox);
        context.RemoveRange(deadOutbox);
        context.RemoveRange(auditLogs);
        context.RemoveRange(notifications);
        var total = passwordTokens.Count + refreshTokens.Count + sentOutbox.Count + deadOutbox.Count + auditLogs.Count + notifications.Count;
        if (total > 0) await context.SaveChangesAsync(cancellationToken);
        return total;
    }

    private async Task AnonymizeAsync(PrivacyRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == null)
        {
            request.Status = "Completed";
            request.CompletedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            request.UserId = null;
            request.Status = "Completed";
            request.CompletedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var originalEmail = user.Email;
        var memberId = user.MemberId;
        var subjectReference = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{user.Id:N}:hcbe-privacy")));
        var anonymousEmail = $"deleted+{user.Id:N}@invalid.local";

        context.RemoveRange(await context.RefreshTokens.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        context.RemoveRange(await context.PasswordResetTokens.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        context.RemoveRange(await context.Notifications.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));

        user.Email = anonymousEmail;
        user.FirstName = "Deleted";
        user.LastName = "Member";
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        user.IsAdmin = false;
        user.IsActive = false;
        user.MemberId = null;

        if (memberId != null)
        {
            var member = await context.Members.SingleOrDefaultAsync(item => item.Id == memberId, cancellationToken);
            if (member != null)
            {
                member.FirstName = "Deleted"; member.LastName = "Member"; member.Email = $"deleted+{member.Id:N}@invalid.local";
                member.Phone = null; member.City = null; member.Province = null; member.Profession = null;
                member.Expertise = null; member.Interests = null; member.Availability = null; member.Zone = null; member.IsAdmin = false;
            }

            var profile = await context.NetworkingProfiles.SingleOrDefaultAsync(item => item.MemberId == memberId, cancellationToken);
            if (profile != null)
            {
                profile.Headline = ""; profile.Bio = ""; profile.Expertise = ""; profile.Sectors = "";
                profile.City = null; profile.Province = null; profile.IsVisible = false; profile.AllowContactRequests = false;
            }

            var mentorship = await context.MentorshipApplications.Where(item => item.MemberId == memberId).ToListAsync(cancellationToken);
            foreach (var item in mentorship)
            {
                item.ProfessionalSummary = "[redacted]"; item.Expertise = "[redacted]"; item.Objectives = "[redacted]";
                item.Availability = ""; item.ConsentToShare = false; item.Status = "Withdrawn"; item.CommitteeNotes = null;
            }
            foreach (var item in await context.ConnectionRequests.Where(item => item.RequesterMemberId == memberId || item.RecipientMemberId == memberId).ToListAsync(cancellationToken))
                item.Message = "[redacted]";
            foreach (var item in await context.PrivateMessages.Where(item => item.SenderMemberId == memberId).ToListAsync(cancellationToken))
                item.Body = "[message removed]";
            foreach (var item in await context.ConversationReports.Where(item => item.ReporterMemberId == memberId).ToListAsync(cancellationToken))
                item.Reason = "[redacted]";
            foreach (var item in await context.EventRegistrations.Where(item => item.MemberId == memberId).ToListAsync(cancellationToken))
            {
                item.AccessibilityNeeds = null;
                item.AdminNotes = null;
            }
            foreach (var item in await context.ServiceCases.Where(item => item.MemberId == memberId).ToListAsync(cancellationToken))
            {
                item.Subject = "[redacted]";
                item.Description = "[redacted]";
                item.InternalNotes = null;
            }
            foreach (var item in await context.ServiceCaseMessages.Where(item => item.AuthorUserId == user.Id).ToListAsync(cancellationToken))
                item.Body = "[message removed]";
            foreach (var item in await context.Associations.Where(item => item.OwnerMemberId == memberId).ToListAsync(cancellationToken))
                item.OwnerMemberId = null;
            foreach (var item in await context.AssociationClaimRequests.Where(item => item.MemberId == memberId).ToListAsync(cancellationToken))
            {
                item.Message = "[redacted]";
                item.AdminNotes = null;
            }
            foreach (var item in await context.OpportunityApplications.Where(item => item.MemberId == memberId).ToListAsync(cancellationToken))
            {
                item.Message = "[redacted]";
                item.AdminNotes = null;
            }
            foreach (var item in await context.MentorshipGoals.Where(item => item.CreatedByMemberId == memberId).ToListAsync(cancellationToken))
                item.Title = "[redacted]";
            foreach (var item in await context.MentorshipCheckIns.Where(item => item.MemberId == memberId).ToListAsync(cancellationToken))
            {
                item.Summary = "[redacted]";
                item.NeedsCommitteeSupport = false;
            }
        }

        foreach (var item in await context.MembershipApplications.Where(item => item.Email == originalEmail || item.MemberId == memberId).ToListAsync(cancellationToken))
        {
            item.FirstName = "Deleted"; item.LastName = "Member"; item.Email = anonymousEmail;
            item.Phone = null; item.City = null; item.Province = null; item.Profession = null;
            item.Expertise = null; item.Motivation = null; item.PasswordHash = null;
        }
        foreach (var item in await context.NewsletterSubscriptions.Where(item => item.Email == originalEmail).ToListAsync(cancellationToken))
        {
            item.Email = $"deleted+{item.Id:N}@invalid.local"; item.FullName = "Deleted member";
            item.IsActive = false; item.UnsubscribeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); item.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var item in await context.EmailOutboxMessages.Where(item => item.Recipient == originalEmail).ToListAsync(cancellationToken))
        {
            item.Recipient = anonymousEmail;
            if (item.Status is "Pending" or "Failed") { item.Status = "Cancelled"; item.LastError = "Cancelled by privacy request"; }
        }
        foreach (var item in await context.AuditLogs.Where(item => item.UserId == user.Id || item.UserEmail == originalEmail).ToListAsync(cancellationToken))
        {
            item.UserId = null; item.UserEmail = null;
        }

        request.SubjectReference = subjectReference;
        request.UserId = null;
        request.Status = "Completed";
        request.CompletedAtUtc = DateTime.UtcNow;
        request.FailureReason = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static PrivacyRequestDto Map(PrivacyRequest item) => new(
        item.Id, item.Type, item.Status, item.RequestedAtUtc, item.ExecuteAfterUtc,
        item.CancelledAtUtc, item.CompletedAtUtc);
}
