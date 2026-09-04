using System.Text;
using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace HcbeApi.Tests.Services;

public sealed class OpportunityParticipationTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.CreateInMemoryContext();
    private readonly Mock<IFileStorageService> _files = new();
    private readonly SilentNotifications _notifications = new();

    public OpportunityParticipationTests()
    {
        _files.SetupGet(x => x.MaxFileSizeBytes).Returns(10_000_000);
        _files.Setup(x => x.IsAllowedExtension(It.IsAny<string>())).Returns(true);
        _files.Setup(x => x.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string?>())).ReturnsAsync(("/api/storage/opportunities/cv.pdf", "cv.pdf"));
        _files.Setup(x => x.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new StoredFileContent(Encoding.UTF8.GetBytes("test resume"), "application/pdf"));
        _files.Setup(x => x.DeleteAsync(It.IsAny<string?>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Matching_RanksSkillsRegionAndAvailability()
    {
        var user = MemberUser("aminata@example.com", "Toronto", "Ontario", "Communication, accueil", "soir");
        _db.Opportunities.AddRange(
            Opportunity("Accueil communautaire", "Communication, accueil", "Ontario", "soir", false),
            Opportunity("Soutien technique", "Kubernetes", "Québec", "jour", false));
        await _db.SaveChangesAsync();
        var service = Service();

        var result = await service.GetMatchedAsync(user.Id, "Volunteer");

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Opportunity.Title.Should().Be("Accueil communautaire");
        result.Data[0].Score.Should().Be(100);
        result.Data[0].Reasons.Should().Contain(["skills", "region", "availability"]);
    }

    [Fact]
    public async Task AcceptedVolunteer_CanSubmitDocumentAndHours_ThenReceivePdfCertificate()
    {
        var user = MemberUser("idrissa@example.com", "Montréal", "Québec", "Coordination", "weekend");
        var opportunity = Opportunity("Collecte communautaire", "Coordination", "Québec", "weekend", false);
        _db.Opportunities.Add(opportunity); await _db.SaveChangesAsync();
        var service = Service();
        var application = await service.ApplyAsync(user.Id, opportunity.Id, new("Je souhaite coordonner les bénévoles pendant la collecte.", "Expérience associative", "Samedi"));
        await service.ReviewApplicationAsync(application.Data!.Id, new("Accepted", null));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test resume"));
        var file = new FormFile(stream, 0, stream.Length, "file", "cv.pdf") { Headers = new HeaderDictionary(), ContentType = "application/pdf" };

        var document = await service.AddApplicationDocumentAsync(user.Id, application.Data.Id, file);
        var downloaded = await service.GetApplicationDocumentAsync(user.Id, application.Data.Id, document.Data!.Id, false);
        var time = await service.AddVolunteerTimeAsync(user.Id, application.Data.Id, new(DateTime.UtcNow.AddDays(-1), 4.5m, "Coordination de la collecte"));
        await service.ReviewVolunteerTimeAsync(Guid.NewGuid(), time.Data!.Id, new("Approved", "Participation confirmée"));
        var certificate = await service.IssueCertificateAsync(Guid.NewGuid(), application.Data.Id, new("Contribution essentielle à l'accueil des participants."));
        var pdf = await service.GetCertificatePdfAsync(user.Id, application.Data.Id, false);

        document.Success.Should().BeTrue();
        downloaded.Data!.FileName.Should().Be("cv.pdf");
        certificate.Success.Should().BeTrue();
        certificate.Data!.ConfirmedHours.Should().Be(4.5m);
        pdf.Success.Should().BeTrue();
        pdf.Data.Should().StartWith(Encoding.ASCII.GetBytes("%PDF-1.4"));
        Encoding.ASCII.GetString(pdf.Data!).Should().Contain(certificate.Data.CertificateNumber).And.Contain("CERTIFICATE OF PARTICIPATION");
    }

    [Fact]
    public async Task VolunteerCertificate_RequiresApprovedHours()
    {
        var user = MemberUser("fatou@example.com", "Ottawa", "Ontario", "Animation", "weekend");
        var opportunity = Opportunity("Animation jeunesse", "Animation", "Ontario", "weekend", false);
        _db.Opportunities.Add(opportunity); await _db.SaveChangesAsync();
        var service = Service();
        var application = await service.ApplyAsync(user.Id, opportunity.Id, new("Je souhaite animer cet atelier pour les jeunes membres.", null, null));
        await service.ReviewApplicationAsync(application.Data!.Id, new("Accepted", null));

        var result = await service.IssueCertificateAsync(Guid.NewGuid(), application.Data.Id, new(null));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Approve at least one");
    }

    private OpportunityService Service() => new(_db, _notifications, _files.Object);
    private User MemberUser(string email, string city, string province, string expertise, string availability)
    {
        var member = new Member { FirstName = "Test", LastName = "Member", Email = email, City = city, Province = province, Expertise = expertise, Availability = availability };
        var user = new User { Email = email, Member = member, MemberId = member.Id, IsActive = true };
        _db.Users.Add(user); return user;
    }
    private static Opportunity Opportunity(string title, string skills, string region, string availability, bool remote) => new()
    {
        Title = title, Description = "Une occasion communautaire détaillée.", Type = "Volunteer", Organization = "HCBE Canada",
        Skills = skills, Region = region, Availability = availability, IsRemote = remote, Status = "Published", DeadlineUtc = DateTime.UtcNow.AddDays(30)
    };
    public void Dispose() => _db.Dispose();

    private sealed class SilentNotifications : INotificationService
    {
        public Task CreateNotificationAsync(string type, string title, string message, Guid? relatedEntityId = null, string? link = null) => Task.CompletedTask;
        public Task CreateForUserAsync(Guid userId, string type, string title, string message, Guid? relatedEntityId = null, string? link = null) => Task.CompletedTask;
        public Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(Guid? userId = null, int limit = 5) => throw new NotSupportedException();
        public Task<ApiResponse<NotificationDto>> MarkAsReadAsync(Guid id, Guid? userId = null) => throw new NotSupportedException();
        public Task<ApiResponse> MarkAllAsReadAsync(Guid? userId = null) => throw new NotSupportedException();
        public Task<ApiResponse<int>> GetUnreadCountAsync(Guid? userId = null) => throw new NotSupportedException();
    }
}
