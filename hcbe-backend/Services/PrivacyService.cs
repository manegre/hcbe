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
    IFileStorageService fileStorage,
    IPaymentGateway paymentGateway,
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
                    item.Id,
                    item.TicketNumber,
                    item.Category,
                    item.Subject,
                    item.Description,
                    item.Status,
                    item.Priority,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.LastResponseAt,
                    item.ResolvedAt,
                    messages = item.Messages.Where(message => !message.IsInternal).Select(message => new
                    {
                        message.Id,
                        message.AuthorUserId,
                        message.Body,
                        message.CreatedAt
                    }),
                    attachments = item.Attachments.Where(attachment => !attachment.IsInternal).Select(attachment => new
                    {
                        attachment.Id,
                        attachment.FileName,
                        attachment.Url,
                        attachment.ContentType,
                        attachment.SizeBytes,
                        attachment.CreatedAt
                    })
                }).ToListAsync(cancellationToken),
            membershipApplications = await context.MembershipApplications.AsNoTracking().Where(item => item.Email == user.Email).Select(item => new
            {
                item.Id,
                item.FirstName,
                item.LastName,
                item.Email,
                item.Phone,
                item.City,
                item.Province,
                item.Profession,
                item.Expertise,
                item.Motivation,
                item.Status,
                item.MemberId,
                item.CreatedAt,
                item.ReviewedAt
            }).ToListAsync(cancellationToken),
            publicSubmissions = await context.PublicSubmissions.AsNoTracking().Where(item => item.Email == user.Email).Select(item => new
            {
                item.Id,
                item.Type,
                item.FirstName,
                item.LastName,
                item.Email,
                item.Phone,
                item.Subject,
                item.City,
                item.Details,
                item.MetadataJson,
                item.Status,
                item.CreatedAt,
                item.ReviewedAt
            }).ToListAsync(cancellationToken),
            newsletterSubscriptions = await context.NewsletterSubscriptions.AsNoTracking().Where(item => item.Email == user.Email).Select(item => new
            {
                item.Id,
                item.Email,
                item.FullName,
                item.PreferredLanguage,
                item.ConsentAcceptedAt,
                item.IsActive,
                item.Source,
                item.CreatedAt,
                item.UpdatedAt
            }).ToListAsync(cancellationToken),
            communicationConsents = await context.CommunicationConsentEvents.AsNoTracking()
                .Where(item => item.UserId == userId || item.Email == user.Email)
                .Select(item => new { item.Category, item.Action, item.Source, item.OccurredAtUtc })
                .ToListAsync(cancellationToken),
            campaignActivity = await context.NewsletterDeliveries.AsNoTracking()
                .Where(item => item.Recipient == user.Email)
                .Select(item => new
                {
                    item.CampaignId,
                    item.QueuedAtUtc,
                    item.FirstOpenedAtUtc,
                    item.LastOpenedAtUtc,
                    item.OpenCount,
                    item.UnsubscribedAtUtc
                }).ToListAsync(cancellationToken),
            consultationComments = await context.ConsultationComments.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new { item.ConsultationId, item.Body, item.CreatedAtUtc }).ToListAsync(cancellationToken),
            consultationParticipations = await context.ConsultationParticipations.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new { item.ConsultationId, item.ParticipatedAtUtc }).ToListAsync(cancellationToken),
            namedConsultationBallots = await context.ConsultationBallots.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new { item.ConsultationId, item.OptionId, item.CastAtUtc }).ToListAsync(cancellationToken),
            membershipStanding = await context.MembershipStandings.AsNoTracking().Where(item => item.UserId == userId).Select(item => new
            {
                item.Status,
                item.PlanId,
                item.CurrentPeriodStartUtc,
                item.CurrentPeriodEndUtc,
                item.GraceEndsAtUtc,
                item.AutoRenew,
                item.UpdatedAtUtc
            }).SingleOrDefaultAsync(cancellationToken),
            financialTransactions = await context.FinancialTransactions.AsNoTracking().Where(item => item.UserId == userId || item.PayerEmail == user.Email).Select(item => new
            {
                item.Id,
                item.Kind,
                item.Status,
                item.AmountCents,
                item.RefundedAmountCents,
                item.Currency,
                item.ReceiptNumber,
                item.MembershipPlanId,
                item.DonationCampaignId,
                item.IsRecurring,
                item.IsAnonymous,
                item.AllowPublicRecognition,
                item.CreatedAtUtc,
                item.PaidAtUtc,
                item.RefundedAtUtc
            }).ToListAsync(cancellationToken),
            ticketOrders = await context.EventTicketOrders.AsNoTracking()
                .Where(item => item.UserId == userId || item.BuyerEmail == user.Email)
                .Select(item => new
                {
                    item.Id,
                    item.EventId,
                    item.OrderNumber,
                    item.BuyerName,
                    item.BuyerEmail,
                    item.Status,
                    item.Currency,
                    item.SubtotalCents,
                    item.DiscountCents,
                    item.PlatformFeeCents,
                    item.TotalCents,
                    item.RefundedAmountCents,
                    item.CreatedAtUtc,
                    item.PaidAtUtc,
                    item.RefundedAtUtc,
                    tickets = item.Tickets.Select(ticket => new
                    {
                        ticket.Id,
                        ticket.TierId,
                        ticket.TicketCode,
                        ticket.AttendeeName,
                        ticket.AttendeeEmail,
                        ticket.Status,
                        ticket.IssuedAtUtc,
                        ticket.CheckedInAtUtc,
                        ticket.TransferredAtUtc
                    })
                }).ToListAsync(cancellationToken),
            organizerProfile = await context.CommunityOrganizers.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new
                {
                    item.Id,
                    item.DisplayName,
                    item.DisplayNameEn,
                    item.ContactEmail,
                    item.ContactPhone,
                    item.WebsiteUrl,
                    item.Description,
                    item.DescriptionEn,
                    item.Status,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    item.ReviewedAtUtc
                }).SingleOrDefaultAsync(cancellationToken),
            advertisingCampaigns = await context.AdvertisingCampaigns.AsNoTracking()
                .Where(item => item.SubmittedByUserId == userId || item.ContactEmail == user.Email)
                .Select(item => new
                {
                    item.Id,
                    item.AdvertiserName,
                    item.ContactEmail,
                    item.Title,
                    item.TitleEn,
                    item.Body,
                    item.BodyEn,
                    item.ImageUrl,
                    item.DestinationUrl,
                    item.Placements,
                    item.TargetLanguage,
                    item.TargetProvince,
                    item.TargetZone,
                    item.Status,
                    item.BudgetCents,
                    item.Currency,
                    item.ImpressionCount,
                    item.ClickCount,
                    item.StartsAtUtc,
                    item.EndsAtUtc,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc
                }).ToListAsync(cancellationToken),
            communityBusinesses = await context.CommunityBusinesses.AsNoTracking()
                .Where(item => item.OwnerUserId == userId)
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.NameEn,
                    item.Category,
                    item.Description,
                    item.DescriptionEn,
                    item.Services,
                    item.ServicesEn,
                    item.ContactEmail,
                    item.ContactPhone,
                    item.WebsiteUrl,
                    item.City,
                    item.Province,
                    item.ServiceRegions,
                    item.Status,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc
                }).ToListAsync(cancellationToken),
            newcomerJourney = await context.NewcomerJourneys.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new
                {
                    item.ArrivalDate,
                    item.City,
                    item.Province,
                    item.PreferredLanguage,
                    item.NeedsJson,
                    item.CompletedStepsJson,
                    item.MentorRequested,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc
                }).SingleOrDefaultAsync(cancellationToken),
            familyHousehold = await context.FamilyHouseholds.AsNoTracking()
                .Where(item => item.OwnerUserId == userId)
                .Select(item => new
                {
                    item.Id,
                    item.HouseholdName,
                    item.Status,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    members = item.Members.Select(member => new
                    {
                        member.Id,
                        member.FullName,
                        member.Relationship,
                        member.Email,
                        member.BirthDate,
                        member.Status,
                        member.CreatedAtUtc
                    })
                }).SingleOrDefaultAsync(cancellationToken),
            appointmentBookings = await context.AppointmentBookings.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new
                {
                    item.Id,
                    item.SlotId,
                    item.Reason,
                    item.Status,
                    item.CreatedAtUtc,
                    item.CancelledAtUtc
                }).ToListAsync(cancellationToken),
            partnerBenefitClaims = await context.PartnerBenefitClaims.AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new
                {
                    item.Id,
                    item.BenefitId,
                    item.RedemptionCode,
                    item.Status,
                    item.ClaimedAtUtc,
                    item.RedeemedAtUtc
                }).ToListAsync(cancellationToken),
            grantApplications = await context.GrantApplications.AsNoTracking()
                .Where(item => item.UserId == userId || item.ApplicantEmail == user.Email)
                .Select(item => new
                {
                    item.Id,
                    item.GrantProgramId,
                    item.ApplicantName,
                    item.ApplicantEmail,
                    item.Statement,
                    item.AnswersJson,
                    item.DocumentsJson,
                    item.Status,
                    item.SubmittedAtUtc,
                    item.UpdatedAtUtc,
                    item.ReviewedAtUtc
                }).ToListAsync(cancellationToken),
            sponsorshipRequests = await context.SponsorshipRequests.AsNoTracking()
                .Where(item => item.UserId == userId || item.ContactEmail == user.Email)
                .Select(item => new
                {
                    item.Id,
                    item.PackageId,
                    item.OrganizationName,
                    item.ContactEmail,
                    item.Objective,
                    item.Notes,
                    item.ProposedAmountCents,
                    item.Currency,
                    item.Status,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    item.ReviewedAtUtc
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

        // Respect the withdrawal immediately while keeping the account recoverable
        // during the grace period. Cancelling deletion never silently opts the member
        // back in; they can make that choice again in the preference centre.
        var preferences = await context.MemberPreferences.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (preferences == null)
        {
            preferences = new MemberPreference { UserId = userId };
            context.MemberPreferences.Add(preferences);
        }

        var preferenceWithdrawals = new (string Category, bool Enabled)[]
        {
            ("events", preferences.EmailEvents),
            ("opportunities", preferences.EmailOpportunities),
            ("mentorship", preferences.EmailMentorship),
            ("service", preferences.EmailServiceUpdates),
            ("newsletter", preferences.EmailNewsletter),
            ("push", preferences.PushNotifications)
        };
        foreach (var (category, enabled) in preferenceWithdrawals.Where(item => item.Enabled))
            context.CommunicationConsentEvents.Add(new CommunicationConsentEvent
            {
                UserId = userId,
                Email = user.Email,
                Category = category,
                Action = "OptOut",
                Source = "account-deletion-request"
            });

        preferences.EmailEvents = false;
        preferences.EmailOpportunities = false;
        preferences.EmailMentorship = false;
        preferences.EmailServiceUpdates = false;
        preferences.EmailNewsletter = false;
        preferences.PushNotifications = false;
        preferences.HasCompletedPreferences = true;
        preferences.UpdatedAt = DateTime.UtcNow;

        if (user.MemberId is Guid memberId)
        {
            var profile = await context.NetworkingProfiles.SingleOrDefaultAsync(item => item.MemberId == memberId, cancellationToken);
            if (profile != null)
            {
                profile.IsVisible = false;
                profile.AllowContactRequests = false;
            }

            foreach (var application in await context.MentorshipApplications
                         .Where(item => item.MemberId == memberId && item.ConsentToShare)
                         .ToListAsync(cancellationToken))
                application.ConsentToShare = false;
        }

        foreach (var subscription in await context.NewsletterSubscriptions
                     .Where(item => item.Email == user.Email && item.IsActive)
                     .ToListAsync(cancellationToken))
        {
            subscription.IsActive = false;
            subscription.UpdatedAt = DateTime.UtcNow;
            context.CommunicationConsentEvents.Add(new CommunicationConsentEvent
            {
                UserId = userId,
                Email = user.Email,
                Category = "newsletter",
                Action = "OptOut",
                Source = "account-deletion-request"
            });
        }

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
                // Do not persist partially anonymized tracked entities when one step
                // fails. The failed request remains visible for operational follow-up
                // and the member can submit it again after the cause is corrected.
                context.ChangeTracker.Clear();
                var failedRequest = await context.PrivacyRequests
                    .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
                if (failedRequest != null)
                {
                    failedRequest.Status = "Failed";
                    failedRequest.FailureReason = exception.Message.Length > 1000 ? exception.Message[..1000] : exception.Message;
                }
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
        var communicationConsents = await context.CommunicationConsentEvents.Where(item => item.OccurredAtUtc < now.AddDays(-auditDays)).ToListAsync(cancellationToken);
        var campaignDeliveries = await context.NewsletterDeliveries.Where(item => item.QueuedAtUtc < now.AddDays(-auditDays)).ToListAsync(cancellationToken);
        var mfaChallenges = await context.MfaChallenges.Where(item => item.ExpiresAtUtc < now.AddDays(-7)).ToListAsync(cancellationToken);
        var stalePushSubscriptions = await context.WebPushSubscriptions.Where(item => item.LastUsedAtUtc < now.AddDays(-180)).ToListAsync(cancellationToken);

        context.RemoveRange(passwordTokens);
        context.RemoveRange(refreshTokens);
        context.RemoveRange(sentOutbox);
        context.RemoveRange(deadOutbox);
        context.RemoveRange(auditLogs);
        context.RemoveRange(notifications);
        context.RemoveRange(communicationConsents);
        context.RemoveRange(campaignDeliveries);
        context.RemoveRange(mfaChallenges);
        context.RemoveRange(stalePushSubscriptions);
        var total = passwordTokens.Count + refreshTokens.Count + sentOutbox.Count + deadOutbox.Count + auditLogs.Count + notifications.Count + communicationConsents.Count + campaignDeliveries.Count + mfaChallenges.Count + stalePushSubscriptions.Count;
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
        var membershipStanding = await context.MembershipStandings.SingleOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        if (membershipStanding?.StripeSubscriptionId is string subscriptionId && paymentGateway.IsEnabled)
        {
            // Do not remove the provider reference until cancellation succeeds; otherwise
            // an anonymized account could continue to renew without a recovery path.
            await paymentGateway.CancelSubscriptionAsync(subscriptionId, cancellationToken);
        }

        context.RemoveRange(await context.RefreshTokens.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        context.RemoveRange(await context.MfaChallenges.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        context.RemoveRange(await context.WebPushSubscriptions.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        context.RemoveRange(await context.PasswordResetTokens.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        context.RemoveRange(await context.Notifications.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));

        user.Email = anonymousEmail;
        user.FirstName = "Deleted";
        user.LastName = "Member";
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        user.IsAdmin = false;
        user.IsActive = false;
        user.MemberId = null;
        user.MfaSecretProtected = null;
        user.MfaRecoveryCodesJson = null;
        user.MfaEnabledAtUtc = null;
        user.MfaMethod = null;

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
            var uploadedAttachments = await context.ServiceCaseAttachments
                .Where(item => item.UploadedByUserId == user.Id)
                .ToListAsync(cancellationToken);
            foreach (var attachment in uploadedAttachments)
            {
                await fileStorage.DeleteAsync(attachment.Url);
            }
            context.RemoveRange(uploadedAttachments);
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
        foreach (var item in await context.PublicSubmissions.Where(item => item.Email == originalEmail).ToListAsync(cancellationToken))
        {
            item.FirstName = "Deleted"; item.LastName = "Member"; item.Email = anonymousEmail;
            item.Phone = null; item.Subject = "[redacted]"; item.City = null;
            item.Details = "[redacted]"; item.MetadataJson = null;
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
        foreach (var item in await context.NewsletterDeliveries.Where(item => item.Recipient == originalEmail).ToListAsync(cancellationToken))
        {
            item.Recipient = anonymousEmail;
            item.TrackingToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        }
        foreach (var item in await context.CommunicationConsentEvents.Where(item => item.UserId == user.Id || item.Email == originalEmail).ToListAsync(cancellationToken))
        {
            item.UserId = null;
            item.Email = anonymousEmail;
        }
        context.RemoveRange(await context.CommunityJourneyStates.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken));
        if (membershipStanding != null)
        {
            membershipStanding.Status = MembershipStatuses.Inactive;
            membershipStanding.AutoRenew = false;
            membershipStanding.StripeCustomerId = null;
            membershipStanding.StripeSubscriptionId = null;
            membershipStanding.LastReminderKey = null;
            membershipStanding.LastReminderAtUtc = null;
            membershipStanding.UpdatedAtUtc = DateTime.UtcNow;
        }
        // Accounting records are retained for legal and reconciliation obligations,
        // but their direct identifiers and public-recognition choices are removed.
        foreach (var item in await context.FinancialTransactions.Where(item => item.UserId == user.Id || item.PayerEmail == originalEmail).ToListAsync(cancellationToken))
        {
            item.UserId = null;
            item.PayerEmail = anonymousEmail;
            item.PayerName = "Deleted member";
            item.DonorMessage = null;
            item.IsAnonymous = true;
            item.AllowPublicRecognition = false;
            item.ReceiptToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            item.UpdatedAtUtc = DateTime.UtcNow;
        }
        // Ticket accounting is retained for reconciliation and refunds, while all
        // attendee identifiers and bearer access tokens are irreversibly replaced.
        var ticketOrders = await context.EventTicketOrders
            .Include(item => item.Tickets)
            .Where(item => item.UserId == user.Id || item.BuyerEmail == originalEmail)
            .ToListAsync(cancellationToken);
        foreach (var order in ticketOrders)
        {
            order.UserId = null;
            order.BuyerName = "Deleted member";
            order.BuyerEmail = anonymousEmail;
            order.AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            order.UpdatedAtUtc = DateTime.UtcNow;
            foreach (var ticket in order.Tickets)
            {
                ticket.AttendeeName = "Deleted member";
                ticket.AttendeeEmail = anonymousEmail;
            }
        }
        var organizer = await context.CommunityOrganizers.SingleOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        if (organizer != null)
        {
            organizer.ContactEmail = anonymousEmail;
            organizer.ContactPhone = null;
            organizer.Description = null;
            organizer.DescriptionEn = null;
            organizer.ReviewNotes = null;
            organizer.Status = OrganizerStatuses.Suspended;
            organizer.UpdatedAtUtc = DateTime.UtcNow;
        }
        foreach (var campaign in await context.AdvertisingCampaigns
                     .Where(item => item.SubmittedByUserId == user.Id || item.ContactEmail == originalEmail)
                     .ToListAsync(cancellationToken))
        {
            campaign.SubmittedByUserId = null;
            campaign.ContactEmail = anonymousEmail;
            campaign.Status = "Paused";
            campaign.ReviewNotes = null;
            campaign.UpdatedAtUtc = DateTime.UtcNow;
        }
        foreach (var business in await context.CommunityBusinesses
                     .Where(item => item.OwnerUserId == user.Id)
                     .ToListAsync(cancellationToken))
        {
            business.ContactEmail = anonymousEmail;
            business.ContactPhone = null;
            business.WebsiteUrl = null;
            business.LogoUrl = null;
            business.City = null;
            business.Province = null;
            business.ServiceRegions = null;
            business.Description = "[redacted]";
            business.DescriptionEn = "[redacted]";
            business.Services = null;
            business.ServicesEn = null;
            business.Status = CommunityProgramStatuses.Withdrawn;
            business.IsFeatured = false;
            business.ReviewNotes = null;
            business.UpdatedAtUtc = DateTime.UtcNow;
        }
        context.RemoveRange(await context.NewcomerJourneys
            .Where(item => item.UserId == user.Id)
            .ToListAsync(cancellationToken));
        var household = await context.FamilyHouseholds
            .Include(item => item.Members)
            .SingleOrDefaultAsync(item => item.OwnerUserId == user.Id, cancellationToken);
        if (household != null)
        {
            context.RemoveRange(household.Members);
            context.FamilyHouseholds.Remove(household);
        }
        foreach (var booking in await context.AppointmentBookings
                     .Where(item => item.UserId == user.Id)
                     .ToListAsync(cancellationToken))
        {
            booking.Reason = null;
            booking.Status = CommunityProgramStatuses.Cancelled;
            booking.CancelledAtUtc ??= DateTime.UtcNow;
        }
        foreach (var claim in await context.PartnerBenefitClaims
                     .Where(item => item.UserId == user.Id)
                     .ToListAsync(cancellationToken))
        {
            claim.RedemptionCode = $"DELETED-{claim.Id:N}";
            claim.Status = CommunityProgramStatuses.Cancelled;
        }
        foreach (var application in await context.GrantApplications
                     .Where(item => item.UserId == user.Id || item.ApplicantEmail == originalEmail)
                     .ToListAsync(cancellationToken))
        {
            application.ApplicantName = "Deleted member";
            application.ApplicantEmail = anonymousEmail;
            application.Statement = "[redacted]";
            application.AnswersJson = "{}";
            application.DocumentsJson = "[]";
            application.AdminNotes = null;
            if (application.Status == CommunityProgramStatuses.Submitted)
                application.Status = CommunityProgramStatuses.Withdrawn;
            application.UpdatedAtUtc = DateTime.UtcNow;
        }
        foreach (var sponsorship in await context.SponsorshipRequests
                     .Where(item => item.UserId == user.Id || item.ContactEmail == originalEmail)
                     .ToListAsync(cancellationToken))
        {
            sponsorship.OrganizationName = "Deleted organization";
            sponsorship.ContactEmail = anonymousEmail;
            sponsorship.Objective = "[redacted]";
            sponsorship.Notes = null;
            if (sponsorship.Status == CommunityProgramStatuses.Submitted)
                sponsorship.Status = CommunityProgramStatuses.Withdrawn;
            sponsorship.UpdatedAtUtc = DateTime.UtcNow;
        }
        foreach (var item in await context.AuditLogs.Where(item => item.UserId == user.Id || item.UserEmail == originalEmail).ToListAsync(cancellationToken))
        {
            item.UserId = null; item.UserEmail = null;
        }
        foreach (var item in await context.ConsultationComments.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken))
        {
            item.UserId = null;
            item.Body = "[comment removed]";
        }
        foreach (var item in await context.ConsultationParticipations.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken))
            item.UserId = null;
        foreach (var item in await context.ConsultationBallots.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken))
            item.UserId = null;
        foreach (var item in await context.ConsultationAuditEvents.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken))
        {
            item.UserId = null;
            item.Details = null;
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
