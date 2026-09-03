using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class EventRegistrationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly EventRegistrationService _service;
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
            configuration);

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

    public void Dispose() => _context.Dispose();
}
