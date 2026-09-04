using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Moq;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class CommunityGrowthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly RecordingNotificationService _notifications = new();

    [Fact]
    public void AdminAccess_UsesRoleDefaults_AndRejectsUnknownPermissions()
    {
        AdminAccess.EffectivePermissions("event-manager", null)
            .Should().BeEquivalentTo([AdminPermissions.DashboardView, AdminPermissions.EventsManage, AdminPermissions.CommunicationsManage]);

        AdminAccess.SerializePermissions([AdminPermissions.ContentManage, "system.owner", AdminPermissions.ContentManage])
            .Should().Be(AdminPermissions.ContentManage);
    }

    [Fact]
    public async Task MemberPreferences_UpdateAdvancesOnboardingWithoutForcingCommunityFeatures()
    {
        var member = new Member { FirstName = "Aminata", LastName = "Ouédraogo", Email = "aminata@example.com" };
        var user = new User { Email = member.Email, Member = member, MemberId = member.Id, IsActive = true };
        _context.Add(user);
        var subscription = new NewsletterSubscription
        {
            Email = member.Email,
            FullName = "Aminata Ouédraogo",
            ConsentAcceptedAt = DateTime.UtcNow,
            IsActive = true,
            UnsubscribeToken = "newsletter-token"
        };
        _context.Add(subscription);
        await _context.SaveChangesAsync();
        var service = new MemberExperienceService(_context);

        var before = await service.GetOnboardingAsync(user.Id);
        var updated = await service.UpdatePreferencesAsync(user.Id, new(
            "en", "America/Toronto", true, false, true, true, false, true));
        var after = await service.GetOnboardingAsync(user.Id);

        before.Data!.CompletionPercent.Should().Be(0);
        before.Data.Preferences.EmailNewsletter.Should().BeFalse();
        updated.Success.Should().BeTrue();
        updated.Data!.HasCompletedPreferences.Should().BeTrue();
        subscription.IsActive.Should().BeFalse();
        after.Data!.CompletionPercent.Should().Be(25);
        after.Data.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task AssociationClaim_ApprovalAssignsOwner_AndRejectsCompetingClaim()
    {
        var first = AddMember("first@example.com");
        var second = AddMember("second@example.com");
        var association = new Association { Name = "Association test", Province = "Québec", City = "Montréal" };
        _context.Add(association);
        await _context.SaveChangesAsync();
        var service = new AssociationPortalService(_context, _notifications, Mock.Of<IFileStorageService>());

        var firstClaim = await service.ClaimAsync(first.Id, association.Id, new("Je représente officiellement cette association communautaire."));
        var secondClaim = await service.ClaimAsync(second.Id, association.Id, new("Je représente aussi cette association communautaire locale."));
        var approved = await service.ReviewAsync(firstClaim.Data!.Id, new("Approved", "Identité vérifiée"));

        approved.Success.Should().BeTrue();
        association.OwnerMemberId.Should().Be(first.MemberId);
        (await _context.AssociationClaimRequests.FindAsync(secondClaim.Data!.Id))!.Status.Should().Be("Rejected");
        _notifications.Count.Should().Be(2);
    }

    [Fact]
    public async Task OrganizationMembership_ApprovalCreatesScopedWorkspaceAccess()
    {
        var owner = AddMember("owner@example.com");
        var applicant = AddMember("applicant@example.com");
        var association = new Association { Name = "Comité entraide", Province = "Ontario", City = "Toronto", OrganizationType = "Committee", OwnerMemberId = owner.MemberId };
        _context.Add(association);
        await _context.SaveChangesAsync();
        var service = new AssociationPortalService(_context, _notifications, Mock.Of<IFileStorageService>());

        var request = await service.JoinAsync(applicant.Id, association.Id, new("Je souhaite contribuer aux activités du comité."));
        var approved = await service.ReviewJoinAsync(owner.Id, association.Id, request.Data!.Id, new("Approved", null, "Editor", "Coordination", ["workspace.view", "documents.manage", "invalid.permission"]));
        var workspace = await service.GetWorkspaceAsync(applicant.Id, association.Id);

        approved.Success.Should().BeTrue();
        workspace.Success.Should().BeTrue();
        workspace.Data!.Access.Role.Should().Be("Editor");
        workspace.Data.Access.Permissions.Should().BeEquivalentTo(["workspace.view", "documents.manage"]);
        workspace.Data.Association.OrganizationType.Should().Be("Committee");
        workspace.Data.Members.Should().Contain(item => item.MemberEmail == "applicant@example.com");
    }

    [Fact]
    public async Task OrganizationWorkspace_OnlyReturnsCasesAssignedToThatOrganization()
    {
        var owner = AddMember("case-owner@example.com");
        var requester = AddMember("requester@example.com");
        var assigned = new Association { Name = "Comité juridique", Province = "Québec", City = "Montréal", OwnerMemberId = owner.MemberId };
        var other = new Association { Name = "Autre comité", Province = "Québec", City = "Québec" };
        _context.AddRange(assigned, other);
        _context.ServiceCases.AddRange(
            new ServiceCase { MemberId = requester.MemberId!.Value, TicketNumber = "HCBE-100", Category = "legal", Subject = "Dossier assigné", Description = "Demande", AssignedAssociation = assigned },
            new ServiceCase { MemberId = requester.MemberId.Value, TicketNumber = "HCBE-200", Category = "other", Subject = "Autre dossier", Description = "Demande", AssignedAssociation = other });
        await _context.SaveChangesAsync();
        var service = new AssociationPortalService(_context, _notifications, Mock.Of<IFileStorageService>());

        var workspace = await service.GetWorkspaceAsync(owner.Id, assigned.Id);

        workspace.Success.Should().BeTrue();
        workspace.Data!.ServiceCases.Should().ContainSingle(item => item.TicketNumber == "HCBE-100");
    }

    [Fact]
    public async Task OpportunityApplication_IsIdempotent_AndInvalidTypesReturnAnError()
    {
        var memberUser = AddMember("volunteer@example.com");
        await _context.SaveChangesAsync();
        var service = new OpportunityService(_context, _notifications);
        var request = new UpsertOpportunityRequest(
            "Accueil des nouveaux membres", null, "Accompagner les nouveaux membres lors des rencontres.", null,
            "Volunteer", "HCBE Canada", "Toronto", false, "Accueil", null, DateTime.UtcNow.AddDays(20), "Published");

        var opportunity = await service.CreateAsync(Guid.NewGuid(), request);
        var first = await service.ApplyAsync(memberUser.Id, opportunity.Data!.Id, new("Je souhaite soutenir l’accueil des nouveaux membres."));
        var duplicate = await service.ApplyAsync(memberUser.Id, opportunity.Data.Id, new("Une seconde soumission ne doit rien créer."));
        var invalid = await service.CreateAsync(Guid.NewGuid(), request with { Type = "Unknown" });

        first.Success.Should().BeTrue();
        duplicate.Data!.Id.Should().Be(first.Data!.Id);
        _context.OpportunityApplications.Should().ContainSingle();
        invalid.Success.Should().BeFalse();
    }

    private User AddMember(string email)
    {
        var member = new Member { FirstName = "Test", LastName = "Member", Email = email };
        var user = new User { Email = email, Member = member, MemberId = member.Id, IsActive = true };
        _context.Add(user);
        return user;
    }

    public void Dispose() => _context.Dispose();

    private sealed class RecordingNotificationService : INotificationService
    {
        public int Count { get; private set; }
        public Task CreateNotificationAsync(string type, string title, string message, Guid? relatedEntityId = null, string? link = null)
        { Count++; return Task.CompletedTask; }
        public Task CreateForUserAsync(Guid userId, string type, string title, string message, Guid? relatedEntityId = null, string? link = null)
        { Count++; return Task.CompletedTask; }
        public Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(Guid? userId = null, int limit = 5) => throw new NotSupportedException();
        public Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid id, Guid? userId = null) => throw new NotSupportedException();
        public Task<ApiResponse> MarkAllAsReadAsync(Guid? userId = null) => throw new NotSupportedException();
        public Task<ApiResponse<int>> GetUnreadCountAsync(Guid? userId = null) => throw new NotSupportedException();
    }
}
