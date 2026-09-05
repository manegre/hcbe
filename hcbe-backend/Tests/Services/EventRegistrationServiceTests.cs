using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class EventRegistrationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly EventRegistrationService _service;
    private readonly Mock<INotificationService> _notifications = new();
    private readonly User _firstUser;
    private readonly User _secondUser;
    private readonly Event _event;

    public EventRegistrationServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppUrl"] = "https://hcbe.ca" })
            .Build();
        _service = new EventRegistrationService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration,
            _notifications.Object);

        var firstMember = new Member { FirstName = "Awa", LastName = "Kaboré", Email = "awa@example.com" };
        var secondMember = new Member { FirstName = "Issa", LastName = "Traoré", Email = "issa@example.com" };
        _firstUser = new User { Email = firstMember.Email, Member = firstMember, MemberId = firstMember.Id, IsActive = true };
        _secondUser = new User { Email = secondMember.Email, Member = secondMember, MemberId = secondMember.Id, IsActive = true };
        _event = new Event
        {
            Title = "Forum de la communauté",
            Date = DateTime.UtcNow.AddDays(4),
            RegistrationDeadline = DateTime.UtcNow.AddDays(3),
            Capacity = 1,
            RegistrationMode = "Native",
            AllowWaitlist = true,
            RestrictMeetingLinkToRegistrants = true,
            MeetingLink = "https://meet.example.com/hcbe",
            Status = "Active"
        };
        _context.AddRange(_firstUser, _secondUser, _event);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RegisterAsync_ConfirmsFirstMember_AndWaitlistsSecond()
    {
        var first = await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest());
        var second = await _service.RegisterAsync(_secondUser.Id, _event.Id, new CreateEventRegistrationRequest());

        first.Success.Should().BeTrue();
        first.Data!.Status.Should().Be("Confirmed");
        first.Data.MeetingLink.Should().Be(_event.MeetingLink);
        second.Success.Should().BeTrue();
        second.Data!.Status.Should().Be("Waitlisted");
        second.Data.MeetingLink.Should().BeNull();
        second.Data.WaitlistPosition.Should().Be(1);
        _context.EmailOutboxMessages.Should().HaveCount(2);
        _notifications.Verify(item => item.CreateForUserAsync(_secondUser.Id, "event-registration",
            It.IsAny<string>(), It.IsAny<string>(), _event.Id, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_PromotesFirstWaitlistedMember()
    {
        await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest());
        await _service.RegisterAsync(_secondUser.Id, _event.Id, new CreateEventRegistrationRequest());

        var cancelled = await _service.CancelAsync(_firstUser.Id, _event.Id);
        var promoted = await _service.GetMineForEventAsync(_secondUser.Id, _event.Id);

        cancelled.Data!.Status.Should().Be("Cancelled");
        promoted.Data!.Status.Should().Be("Confirmed");
        promoted.Data.MeetingLink.Should().Be(_event.MeetingLink);
        _context.EmailOutboxMessages.Should().HaveCount(4);
    }

    [Fact]
    public async Task RegisterAsync_RejectsClosedAndExternalEvents()
    {
        _event.RegistrationMode = "External";
        await _context.SaveChangesAsync();
        var external = await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest());

        _event.RegistrationMode = "Native";
        _event.RegistrationDeadline = DateTime.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();
        var closed = await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest());

        external.Success.Should().BeFalse();
        closed.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CheckInByCodeAsync_MarksConfirmedParticipantAsAttended()
    {
        var registered = (await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest())).Data!;

        var result = await _service.CheckInByCodeAsync(_event.Id, registered.ConfirmationCode.ToLowerInvariant());

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Attended");
        result.Data.CheckedInAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Attendee_CanSubmitSurveyAndDownloadCertificate()
    {
        var registration = (await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest())).Data!;
        await _service.CheckInByCodeAsync(_event.Id, registration.ConfirmationCode);
        _event.Date = DateTime.UtcNow.AddMinutes(-5);
        await _context.SaveChangesAsync();

        var survey = await _service.SubmitSurveyAsync(_firstUser.Id, _event.Id, new SubmitEventSurveyRequest(5, "Excellent accueil", true));
        var stats = await _service.GetStatsAsync(_event.Id);
        var certificate = await _service.BuildCertificateAsync(_firstUser.Id, _event.Id);

        survey.Success.Should().BeTrue();
        survey.Data!.Rating.Should().Be(5);
        stats.Data!.AttendanceRate.Should().Be(100);
        stats.Data.AverageRating.Should().Be(5);
        System.Text.Encoding.ASCII.GetString(certificate.Content![..4]).Should().Be("%PDF");
        certificate.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task AdminCommunication_QueuesOnlySelectedAudience()
    {
        await _service.RegisterAsync(_firstUser.Id, _event.Id, new CreateEventRegistrationRequest());
        await _service.RegisterAsync(_secondUser.Id, _event.Id, new CreateEventRegistrationRequest());

        var result = await _service.SendCommunicationAsync(_firstUser.Id, _event.Id,
            new SendEventCommunicationRequest("Waitlisted", "Mise à jour", "Une place pourrait bientôt se libérer."));

        result.Success.Should().BeTrue();
        result.Data!.RecipientCount.Should().Be(1);
        (await _service.GetCommunicationsAsync(_event.Id)).Data.Should().ContainSingle();
        _context.EmailOutboxMessages.Should().HaveCount(3);
    }

    public void Dispose() => _context.Dispose();
}
