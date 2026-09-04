using Microsoft.EntityFrameworkCore;
using HcbeApi.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;

namespace HcbeApi.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<MemberPreference> MemberPreferences { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<MemberProfile> MemberProfiles { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventSpeaker> EventSpeakers { get; set; }
    public DbSet<EventOrganizer> EventOrganizers { get; set; }
    public DbSet<EventCategory> EventCategories { get; set; }
    public DbSet<EventMedia> EventMedia { get; set; }
    public DbSet<EventAttachment> EventAttachments { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<NewsAttachment> NewsAttachments { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<PageSection> PageSections { get; set; }
    public DbSet<ServiceContent> ServiceContents { get; set; }
    public DbSet<Statistic> Statistics { get; set; }
    public DbSet<NavigationItem> NavigationItems { get; set; }
    public DbSet<FooterLink> FooterLinks { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Association> Associations { get; set; }
    public DbSet<AssociationClaimRequest> AssociationClaimRequests { get; set; }
    public DbSet<Opportunity> Opportunities { get; set; }
    public DbSet<OpportunityApplication> OpportunityApplications { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<MembershipApplication> MembershipApplications { get; set; }
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }
    public DbSet<GrantProgram> GrantPrograms { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<PublicSubmission> PublicSubmissions { get; set; }
    public DbSet<NewsletterCampaign> NewsletterCampaigns { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<MentorshipApplication> MentorshipApplications { get; set; }
    public DbSet<MentorshipMatch> MentorshipMatches { get; set; }
    public DbSet<MentorshipGoal> MentorshipGoals { get; set; }
    public DbSet<MentorshipCheckIn> MentorshipCheckIns { get; set; }
    public DbSet<NetworkingProfile> NetworkingProfiles { get; set; }
    public DbSet<ConnectionRequest> ConnectionRequests { get; set; }
    public DbSet<PrivateConversation> PrivateConversations { get; set; }
    public DbSet<PrivateMessage> PrivateMessages { get; set; }
    public DbSet<ConversationReport> ConversationReports { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<EmailOutboxMessage> EmailOutboxMessages { get; set; }
    public DbSet<PrivacyRequest> PrivacyRequests { get; set; }
    public DbSet<CmsContentItem> CmsContentItems { get; set; }
    public DbSet<CmsContentRevision> CmsContentRevisions { get; set; }
    public DbSet<ServiceCase> ServiceCases { get; set; }
    public DbSet<ServiceCaseMessage> ServiceCaseMessages { get; set; }
    public DbSet<ServiceCaseAttachment> ServiceCaseAttachments { get; set; }
    public DbSet<ErrorIncident> ErrorIncidents { get; set; }
    public DbSet<MembershipPlan> MembershipPlans { get; set; }
    public DbSet<MembershipStanding> MembershipStandings { get; set; }
    public DbSet<DonationCampaign> DonationCampaigns { get; set; }
    public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure indexes
        modelBuilder.Entity<Member>()
            .HasIndex(m => m.Email)
            .IsUnique();

        modelBuilder.Entity<Member>()
            .HasIndex(m => m.Zone);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Member)
            .WithOne()
            .HasForeignKey<User>(u => u.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.MemberId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>().Property(u => u.AdminRole).HasMaxLength(50).HasDefaultValue("super-admin");
        modelBuilder.Entity<User>().Property(u => u.AdminPermissions).HasMaxLength(1000);

        modelBuilder.Entity<MemberPreference>().HasKey(item => item.UserId);
        modelBuilder.Entity<MemberPreference>()
            .HasOne(item => item.User).WithOne().HasForeignKey<MemberPreference>(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MemberPreference>().Property(item => item.PreferredLanguage).HasMaxLength(5);
        modelBuilder.Entity<MemberPreference>().Property(item => item.TimeZone).HasMaxLength(100);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => new { token.UserId, token.ExpiresAtUtc });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => log.CreatedAtUtc);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => new { log.UserId, log.CreatedAtUtc });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => new { log.EntityType, log.EntityId });

        modelBuilder.Entity<EmailOutboxMessage>()
            .HasIndex(message => new { message.Status, message.NextAttemptAtUtc });

        modelBuilder.Entity<EmailOutboxMessage>()
            .HasIndex(message => new { message.RelatedEntityType, message.RelatedEntityId });

        modelBuilder.Entity<ErrorIncident>().Property(item => item.Fingerprint).HasMaxLength(64);
        modelBuilder.Entity<ErrorIncident>().Property(item => item.TraceId).HasMaxLength(200);
        modelBuilder.Entity<ErrorIncident>().Property(item => item.HttpMethod).HasMaxLength(10);
        modelBuilder.Entity<ErrorIncident>().Property(item => item.Path).HasMaxLength(1000);
        modelBuilder.Entity<ErrorIncident>().Property(item => item.ExceptionType).HasMaxLength(500);
        modelBuilder.Entity<ErrorIncident>().Property(item => item.Message).HasMaxLength(2000);
        modelBuilder.Entity<ErrorIncident>().Property(item => item.StackTrace).HasMaxLength(8000);
        modelBuilder.Entity<ErrorIncident>()
            .HasIndex(item => new { item.ResolvedAtUtc, item.LastOccurredAtUtc });
        modelBuilder.Entity<ErrorIncident>()
            .HasIndex(item => item.Fingerprint);

        modelBuilder.Entity<PrivacyRequest>()
            .HasIndex(request => new { request.Status, request.ExecuteAfterUtc });

        modelBuilder.Entity<PrivacyRequest>()
            .HasIndex(request => request.UserId);

        modelBuilder.Entity<MembershipPlan>().Property(item => item.Name).HasMaxLength(160);
        modelBuilder.Entity<MembershipPlan>().Property(item => item.Currency).HasMaxLength(3);
        modelBuilder.Entity<MembershipPlan>().Property(item => item.BillingMode).HasMaxLength(20);
        modelBuilder.Entity<MembershipPlan>().Property(item => item.StripePriceId).HasMaxLength(255);
        modelBuilder.Entity<MembershipPlan>().HasIndex(item => new { item.IsActive, item.DisplayOrder });

        modelBuilder.Entity<MembershipStanding>()
            .HasOne(item => item.User).WithOne().HasForeignKey<MembershipStanding>(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MembershipStanding>()
            .HasOne(item => item.Plan).WithMany().HasForeignKey(item => item.PlanId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MembershipStanding>().HasIndex(item => item.UserId).IsUnique();
        modelBuilder.Entity<MembershipStanding>().HasIndex(item => new { item.Status, item.CurrentPeriodEndUtc });
        modelBuilder.Entity<MembershipStanding>().Property(item => item.Status).HasMaxLength(30);

        modelBuilder.Entity<DonationCampaign>().HasIndex(item => item.Slug).IsUnique();
        modelBuilder.Entity<DonationCampaign>().HasIndex(item => new { item.IsPublished, item.StartsAtUtc, item.EndsAtUtc });
        modelBuilder.Entity<DonationCampaign>().Property(item => item.Currency).HasMaxLength(3);

        modelBuilder.Entity<FinancialTransaction>()
            .HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FinancialTransaction>()
            .HasOne(item => item.MembershipPlan).WithMany().HasForeignKey(item => item.MembershipPlanId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FinancialTransaction>()
            .HasOne(item => item.DonationCampaign).WithMany().HasForeignKey(item => item.DonationCampaignId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FinancialTransaction>().HasIndex(item => item.StripeCheckoutSessionId).IsUnique();
        modelBuilder.Entity<FinancialTransaction>().HasIndex(item => item.StripeInvoiceId).IsUnique();
        modelBuilder.Entity<FinancialTransaction>().HasIndex(item => item.ReceiptNumber).IsUnique();
        modelBuilder.Entity<FinancialTransaction>().HasIndex(item => item.ReceiptToken).IsUnique();
        modelBuilder.Entity<FinancialTransaction>().HasIndex(item => new { item.Status, item.CreatedAtUtc });
        modelBuilder.Entity<FinancialTransaction>().HasIndex(item => new { item.UserId, item.CreatedAtUtc });
        modelBuilder.Entity<FinancialTransaction>().Property(item => item.Kind).HasMaxLength(30);
        modelBuilder.Entity<FinancialTransaction>().Property(item => item.Status).HasMaxLength(30);
        modelBuilder.Entity<FinancialTransaction>().Property(item => item.Currency).HasMaxLength(3);
        modelBuilder.Entity<FinancialTransaction>().Property(item => item.ReceiptNumber).HasMaxLength(40);
        modelBuilder.Entity<FinancialTransaction>().Property(item => item.ReceiptToken).HasMaxLength(96);

        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(item => item.ProviderEventId).IsUnique();
        modelBuilder.Entity<PaymentWebhookEvent>().HasIndex(item => new { item.Status, item.ReceivedAtUtc });
        modelBuilder.Entity<PaymentWebhookEvent>().Property(item => item.ProviderEventId).HasMaxLength(255);
        modelBuilder.Entity<PaymentWebhookEvent>().Property(item => item.EventType).HasMaxLength(120);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Date);

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<EventSpeaker>()
            .HasOne(s => s.Event)
            .WithMany(e => e.Speakers)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventSpeaker>()
            .Property(s => s.Name)
            .HasMaxLength(160);

        modelBuilder.Entity<EventSpeaker>()
            .HasIndex(s => new { s.EventId, s.DisplayOrder });

        modelBuilder.Entity<EventOrganizer>()
            .HasOne(organizer => organizer.Event)
            .WithMany(e => e.Organizers)
            .HasForeignKey(organizer => organizer.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventOrganizer>()
            .Property(organizer => organizer.Name)
            .HasMaxLength(160);

        modelBuilder.Entity<EventOrganizer>()
            .HasIndex(organizer => new { organizer.EventId, organizer.DisplayOrder });

        modelBuilder.Entity<EventCategory>()
            .Property(category => category.Slug)
            .HasMaxLength(80);

        modelBuilder.Entity<EventCategory>()
            .Property(category => category.Name)
            .HasMaxLength(120);

        modelBuilder.Entity<EventCategory>()
            .Property(category => category.NameEn)
            .HasMaxLength(120);

        modelBuilder.Entity<EventCategory>()
            .HasIndex(category => category.Slug)
            .IsUnique();

        modelBuilder.Entity<EventCategory>()
            .HasIndex(category => category.DisplayOrder);

        modelBuilder.Entity<EventMedia>()
            .HasOne(m => m.Event)
            .WithMany(e => e.Media)
            .HasForeignKey(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventMedia>()
            .HasIndex(m => m.EventId);

        modelBuilder.Entity<EventAttachment>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Attachments)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventAttachment>()
            .HasIndex(a => a.EventId);

        modelBuilder.Entity<EventRegistration>()
            .HasOne(registration => registration.Event)
            .WithMany(eventEntity => eventEntity.Registrations)
            .HasForeignKey(registration => registration.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventRegistration>()
            .HasOne(registration => registration.Member)
            .WithMany()
            .HasForeignKey(registration => registration.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EventRegistration>()
            .HasIndex(registration => new { registration.EventId, registration.MemberId })
            .IsUnique();

        modelBuilder.Entity<EventRegistration>()
            .HasIndex(registration => new { registration.EventId, registration.Status, registration.RegisteredAt });

        modelBuilder.Entity<EventRegistration>()
            .HasIndex(registration => registration.ConfirmationCode)
            .IsUnique();

        modelBuilder.Entity<ServiceCase>()
            .HasOne(item => item.Member).WithMany().HasForeignKey(item => item.MemberId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceCase>()
            .HasOne(item => item.AssignedToUser).WithMany().HasForeignKey(item => item.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ServiceCase>()
            .HasIndex(item => item.TicketNumber).IsUnique();
        modelBuilder.Entity<ServiceCase>()
            .HasIndex(item => new { item.Status, item.Priority, item.UpdatedAt });
        modelBuilder.Entity<ServiceCase>()
            .HasIndex(item => new { item.MemberId, item.UpdatedAt });
        modelBuilder.Entity<ServiceCaseMessage>()
            .HasOne(item => item.ServiceCase).WithMany(item => item.Messages).HasForeignKey(item => item.ServiceCaseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ServiceCaseMessage>()
            .HasOne(item => item.AuthorUser).WithMany().HasForeignKey(item => item.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceCaseMessage>()
            .HasIndex(item => new { item.ServiceCaseId, item.CreatedAt });
        modelBuilder.Entity<ServiceCaseAttachment>()
            .HasOne(item => item.ServiceCase).WithMany(item => item.Attachments).HasForeignKey(item => item.ServiceCaseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ServiceCaseAttachment>()
            .HasIndex(item => item.ServiceCaseId);

        modelBuilder.Entity<News>()
            .HasIndex(n => n.PublishedDate);

        modelBuilder.Entity<News>()
            .HasIndex(n => n.Status);

        modelBuilder.Entity<NewsAttachment>()
            .HasOne(a => a.News)
            .WithMany(n => n.Attachments)
            .HasForeignKey(a => a.NewsId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NewsAttachment>()
            .HasIndex(a => a.NewsId);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<Statistic>()
            .HasIndex(s => s.Key)
            .IsUnique();

        modelBuilder.Entity<SiteSetting>()
            .HasIndex(s => s.Key)
            .IsUnique();

        modelBuilder.Entity<CmsContentItem>()
            .HasIndex(item => item.Key)
            .IsUnique();
        modelBuilder.Entity<CmsContentItem>()
            .HasIndex(item => item.ScheduledPublishAtUtc);

        modelBuilder.Entity<CmsContentItem>()
            .HasIndex(item => new { item.Page, item.Section });

        modelBuilder.Entity<CmsContentRevision>()
            .HasOne(revision => revision.CmsContentItem)
            .WithMany(item => item.Revisions)
            .HasForeignKey(revision => revision.CmsContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CmsContentRevision>()
            .HasIndex(revision => new { revision.CmsContentItemId, revision.Version })
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.UserId);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.CreatedAt);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<Partner>()
            .HasIndex(partner => partner.DisplayOrder);

        modelBuilder.Entity<Partner>()
            .HasIndex(partner => new { partner.IsActive, partner.IsFeatured });

        // JSON conversion for list properties
        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) => left!.SequenceEqual(right!),
            values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
            values => values.ToList());

        var projectPartnersProperty = modelBuilder.Entity<Project>()
            .Property(p => p.Partners)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            );
        projectPartnersProperty.Metadata.SetValueComparer(stringListComparer);

        modelBuilder.Entity<Association>()
            .HasOne(item => item.OwnerMember).WithMany().HasForeignKey(item => item.OwnerMemberId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AssociationClaimRequest>()
            .HasOne(item => item.Association).WithMany().HasForeignKey(item => item.AssociationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssociationClaimRequest>()
            .HasOne(item => item.Member).WithMany().HasForeignKey(item => item.MemberId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssociationClaimRequest>()
            .HasIndex(item => new { item.AssociationId, item.MemberId, item.Status });
        modelBuilder.Entity<Opportunity>().HasIndex(item => new { item.Status, item.DeadlineUtc });
        modelBuilder.Entity<OpportunityApplication>()
            .HasOne(item => item.Opportunity).WithMany(item => item.Applications).HasForeignKey(item => item.OpportunityId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OpportunityApplication>()
            .HasOne(item => item.Member).WithMany().HasForeignKey(item => item.MemberId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OpportunityApplication>().HasIndex(item => new { item.OpportunityId, item.MemberId }).IsUnique();

        var associationDomainsProperty = modelBuilder.Entity<Association>()
            .Property(a => a.Domains)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            );
        associationDomainsProperty.Metadata.SetValueComparer(stringListComparer);

        var associationDomainsEnProperty = modelBuilder.Entity<Association>()
            .Property(a => a.DomainsEn)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            );
        associationDomainsEnProperty.Metadata.SetValueComparer(stringListComparer);

        // TeamMember indexes
        modelBuilder.Entity<TeamMember>()
            .HasIndex(t => t.Order);

        modelBuilder.Entity<TeamMember>()
            .HasIndex(t => t.IsActive);

        modelBuilder.Entity<MembershipApplication>()
            .HasIndex(a => a.Email);

        modelBuilder.Entity<MembershipApplication>()
            .HasIndex(a => a.Status);

        modelBuilder.Entity<MembershipApplication>()
            .HasIndex(a => a.CreatedAt);

        modelBuilder.Entity<MembershipApplication>()
            .HasOne(a => a.Member)
            .WithMany()
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(s => s.IsActive);

        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(s => s.PreferredLanguage);

        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(s => s.UnsubscribeToken)
            .IsUnique();

        modelBuilder.Entity<NewsletterCampaign>()
            .HasIndex(c => c.CreatedAt);
        modelBuilder.Entity<NewsletterCampaign>()
            .HasIndex(c => new { c.Status, c.ScheduledAtUtc });

        // Store type is string? so SQLite NULL (legacy rows after ALTER) maps cleanly
        // before conversion, instead of throwing on GetString.
        var grantCriteriaProperty = modelBuilder.Entity<GrantProgram>()
            .Property(g => g.EligibilityCriteria)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<string>(), (JsonSerializerOptions?)null),
                (string? v) => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            );
        grantCriteriaProperty.Metadata.SetValueComparer(stringListComparer);

        var grantCriteriaEnProperty = modelBuilder.Entity<GrantProgram>()
            .Property(g => g.EligibilityCriteriaEn)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<string>(), (JsonSerializerOptions?)null),
                (string? v) => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : (JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            );
        grantCriteriaEnProperty.Metadata.SetValueComparer(stringListComparer);

        modelBuilder.Entity<GrantProgram>()
            .HasIndex(g => g.DisplayOrder);

        modelBuilder.Entity<GrantProgram>()
            .HasIndex(g => g.IsActive);

        modelBuilder.Entity<Consultation>()
            .HasIndex(c => c.DisplayOrder);

        modelBuilder.Entity<Consultation>()
            .HasIndex(c => c.IsActive);

        modelBuilder.Entity<PublicSubmission>()
            .HasIndex(s => new { s.Type, s.Status, s.CreatedAt });

        modelBuilder.Entity<MentorshipApplication>()
            .HasOne(item => item.Member).WithMany().HasForeignKey(item => item.MemberId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MentorshipApplication>()
            .HasIndex(item => new { item.MemberId, item.Role, item.Status });
        modelBuilder.Entity<MentorshipMatch>()
            .HasOne(item => item.MentorApplication).WithMany().HasForeignKey(item => item.MentorApplicationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MentorshipMatch>()
            .HasOne(item => item.MenteeApplication).WithMany().HasForeignKey(item => item.MenteeApplicationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MentorshipMatch>()
            .HasIndex(item => item.Status);
        modelBuilder.Entity<MentorshipGoal>().HasOne(item => item.Match).WithMany().HasForeignKey(item => item.MatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MentorshipGoal>().HasIndex(item => new { item.MatchId, item.Status });
        modelBuilder.Entity<MentorshipCheckIn>().HasOne(item => item.Match).WithMany().HasForeignKey(item => item.MatchId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MentorshipCheckIn>().HasOne(item => item.Member).WithMany().HasForeignKey(item => item.MemberId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MentorshipCheckIn>().HasIndex(item => new { item.MatchId, item.CreatedAt });

        modelBuilder.Entity<NetworkingProfile>()
            .HasOne(item => item.Member).WithOne().HasForeignKey<NetworkingProfile>(item => item.MemberId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NetworkingProfile>()
            .HasIndex(item => item.MemberId).IsUnique();
        modelBuilder.Entity<NetworkingProfile>()
            .HasIndex(item => new { item.IsVisible, item.AllowContactRequests });
        modelBuilder.Entity<ConnectionRequest>()
            .HasOne(item => item.RequesterMember).WithMany().HasForeignKey(item => item.RequesterMemberId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConnectionRequest>()
            .HasOne(item => item.RecipientMember).WithMany().HasForeignKey(item => item.RecipientMemberId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConnectionRequest>()
            .HasIndex(item => new { item.RecipientMemberId, item.Status, item.CreatedAt });

        modelBuilder.Entity<PrivateConversation>()
            .HasOne(item => item.MemberOne).WithMany().HasForeignKey(item => item.MemberOneId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PrivateConversation>()
            .HasOne(item => item.MemberTwo).WithMany().HasForeignKey(item => item.MemberTwoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PrivateConversation>()
            .HasIndex(item => new { item.MemberOneId, item.MemberTwoId }).IsUnique();
        modelBuilder.Entity<PrivateConversation>()
            .HasIndex(item => item.LastMessageAt);
        modelBuilder.Entity<PrivateMessage>()
            .HasOne(item => item.Conversation).WithMany().HasForeignKey(item => item.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PrivateMessage>()
            .HasOne(item => item.SenderMember).WithMany().HasForeignKey(item => item.SenderMemberId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PrivateMessage>()
            .HasIndex(item => new { item.ConversationId, item.CreatedAt });
        modelBuilder.Entity<ConversationReport>()
            .HasOne(item => item.Conversation).WithMany().HasForeignKey(item => item.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ConversationReport>()
            .HasOne(item => item.ReporterMember).WithMany().HasForeignKey(item => item.ReporterMemberId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConversationReport>()
            .HasIndex(item => new { item.Status, item.CreatedAt });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddAuditEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddAuditEntries()
    {
        ChangeTracker.DetectChanges();
        var changedEntries = ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditLog and not RefreshToken and not EmailOutboxMessage &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (changedEntries.Count == 0) return;

        var httpContext = _httpContextAccessor?.HttpContext;
        var userIdValue = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : (Guid?)null;
        var userEmail = httpContext?.User.FindFirstValue(ClaimTypes.Email);

        foreach (var entry in changedEntries)
        {
            var action = entry.State.ToString();
            var primaryKey = entry.Properties.FirstOrDefault(property => property.Metadata.IsPrimaryKey());
            var changedProperties = entry.Properties
                .Where(property => entry.State != EntityState.Modified || property.IsModified)
                .ToDictionary(
                    property => property.Metadata.Name,
                    property => SensitiveProperty(property.Metadata.Name)
                        ? "[REDACTED]"
                        : Truncate(entry.State == EntityState.Deleted
                            ? property.OriginalValue?.ToString()
                            : property.CurrentValue?.ToString()),
                    StringComparer.Ordinal);

            AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = (entry.State == EntityState.Deleted
                    ? primaryKey?.OriginalValue
                    : primaryKey?.CurrentValue)?.ToString(),
                ChangesJson = JsonSerializer.Serialize(changedProperties),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                TraceId = httpContext?.TraceIdentifier
            });
        }
    }

    private static bool SensitiveProperty(string name) =>
        name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Secret", StringComparison.OrdinalIgnoreCase);

    private static string? Truncate(string? value) =>
        value is { Length: > 250 } ? value[..250] + "…" : value;
}

