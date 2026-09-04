using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class ServiceCaseServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly ServiceCaseService _service;
    private readonly User _memberUser;
    private readonly User _adminUser;

    public ServiceCaseServiceTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppUrl"] = "https://hcbe.ca" }).Build();
        var files = new Mock<IFileStorageService>();
        files.Setup(item => item.IsAllowedExtension(It.IsAny<string>())).Returns(true);
        _service = new ServiceCaseService(
            _context,
            Mock.Of<INotificationService>(),
            files.Object,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration);

        var member = new Member { FirstName = "Aminata", LastName = "Ouédraogo", Email = "aminata@example.com" };
        _memberUser = new User { Email = member.Email, Member = member, MemberId = member.Id, IsActive = true };
        _adminUser = new User { Email = "admin@example.com", FirstName = "Admin", LastName = "HCBE", IsActive = true, IsAdmin = true };
        _context.AddRange(_memberUser, _adminUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_CreatesTrackableTicketAndReceiptEmail()
    {
        var result = await _service.CreateAsync(_memberUser.Id, new CreateServiceCaseRequest("integration", "Besoin d'orientation", "Je viens de m'installer et je cherche les premières démarches."));

        result.Success.Should().BeTrue();
        result.Data!.TicketNumber.Should().StartWith("HCBE-");
        result.Data.Status.Should().Be("Submitted");
        _context.EmailOutboxMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task InternalMessages_AreHiddenFromMemberView()
    {
        var created = await _service.CreateAsync(_memberUser.Id, new CreateServiceCaseRequest("legal", "Question de documents", "Je souhaite vérifier les documents nécessaires à ma démarche."));
        _context.ChangeTracker.Clear();
        await _service.AddAdminMessageAsync(_adminUser.Id, created.Data!.Id, new AddServiceCaseMessageRequest("À assigner au comité juridique", true));
        _context.ChangeTracker.Clear();
        await _service.AddAdminMessageAsync(_adminUser.Id, created.Data.Id, new AddServiceCaseMessageRequest("Nous avons bien reçu votre demande.", false));
        _context.ChangeTracker.Clear();

        var memberView = await _service.GetMineByIdAsync(_memberUser.Id, created.Data.Id);
        var adminView = await _service.GetForAdminByIdAsync(created.Data.Id);

        memberView.Data!.Messages.Should().ContainSingle().Which.IsInternal.Should().BeFalse();
        adminView.Data!.Messages.Should().HaveCount(2);
        memberView.Data.Status.Should().Be("AwaitingMember");
    }

    [Fact]
    public async Task AdminCanAssignAndResolveCase()
    {
        var created = await _service.CreateAsync(_memberUser.Id, new CreateServiceCaseRequest("employment", "Recherche d'emploi", "Je souhaite être orientée vers les ressources professionnelles disponibles."));
        _context.ChangeTracker.Clear();

        var result = await _service.UpdateForAdminAsync(created.Data!.Id, new UpdateServiceCaseRequest("Resolved", "High", _adminUser.Id));

        result.Success.Should().BeTrue();
        result.Data!.AssignedToUserId.Should().Be(_adminUser.Id);
        result.Data.Status.Should().Be("Resolved");
        result.Data.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminCanAssignCaseToActiveOrganization()
    {
        var organization = new Association { Name = "Comité emploi", City = "Montréal", Province = "Québec", IsActive = true, OrganizationType = "Committee" };
        _context.Associations.Add(organization);
        await _context.SaveChangesAsync();
        var created = await _service.CreateAsync(_memberUser.Id, new CreateServiceCaseRequest("employment", "Accompagnement professionnel", "Je souhaite être orientée vers un comité spécialisé dans l'emploi."));
        _context.ChangeTracker.Clear();

        var result = await _service.UpdateForAdminAsync(created.Data!.Id, new UpdateServiceCaseRequest(AssignedAssociationId: organization.Id));

        result.Success.Should().BeTrue();
        result.Data!.AssignedAssociationId.Should().Be(organization.Id);
        result.Data.AssignedAssociationName.Should().Be("Comité emploi");
        result.Data.Status.Should().Be("InReview");
    }

    [Fact]
    public async Task AdminCannotAssignCaseToInactiveOrganization()
    {
        var organization = new Association { Name = "Comité inactif", City = "Québec", Province = "Québec", IsActive = false };
        _context.Associations.Add(organization);
        await _context.SaveChangesAsync();
        var created = await _service.CreateAsync(_memberUser.Id, new CreateServiceCaseRequest("other", "Demande communautaire", "Je souhaite être orientée vers une organisation de la communauté."));
        _context.ChangeTracker.Clear();

        var result = await _service.UpdateForAdminAsync(created.Data!.Id, new UpdateServiceCaseRequest(AssignedAssociationId: organization.Id));

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Assigned organization must be active");
    }

    public void Dispose() => _context.Dispose();
}
