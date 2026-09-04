using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class MemberEngagementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly MemberEngagementService _service;
    private readonly User _user;

    public MemberEngagementServiceTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppUrl"] = "https://hcbe.ca" }).Build();
        var member = new Member { FirstName = "Awa", LastName = "Kaboré", Email = "awa@engagement.test" };
        _user = new User { Email = member.Email, MemberId = member.Id, Member = member, IsActive = true };
        _context.Users.Add(_user);
        _context.SaveChanges();
        _service = new MemberEngagementService(_context, new EmailOutbox(_context), new EmailTemplateRenderer(configuration), configuration);
    }

    [Fact]
    public async Task SaveAsync_IsIdempotent_AndResolvesPublishedOpportunity()
    {
        var opportunity = new Opportunity { Title = "Bénévolat", Description = "Aider", Type = "Volunteer", Organization = "HCBE", Status = "Published" };
        _context.Opportunities.Add(opportunity); await _context.SaveChangesAsync();

        (await _service.SaveAsync(_user.Id, "opportunity", opportunity.Id)).Success.Should().BeTrue();
        (await _service.SaveAsync(_user.Id, "Opportunity", opportunity.Id)).Success.Should().BeTrue();

        _context.SavedMemberItems.Should().ContainSingle();
        (await _service.GetSavedAsync(_user.Id)).Data.Should().ContainSingle(item => item.Title == "Bénévolat");
    }

    [Fact]
    public async Task BlockAsync_IsExplicitAndReversible()
    {
        var other = new Member { FirstName = "Issa", LastName = "Traoré", Email = "issa@engagement.test" };
        _context.Members.Add(other); await _context.SaveChangesAsync();

        (await _service.BlockAsync(_user.Id, other.Id)).Success.Should().BeTrue();
        (await _service.GetBlocksAsync(_user.Id)).Data.Should().ContainSingle(item => item.MemberId == other.Id);
        (await _service.UnblockAsync(_user.Id, other.Id)).Success.Should().BeTrue();
        _context.MemberBlocks.Should().BeEmpty();
    }

    [Fact]
    public async Task Reminder_CreatesPrivateNotification_WithoutEmailWhenOptedOut()
    {
        var eventEntity = new Event { Title = "Atelier", Date = DateTime.UtcNow.AddHours(12), Status = "Active", RegistrationMode = "Native" };
        _context.Events.Add(eventEntity);
        _context.EventRegistrations.Add(new EventRegistration { Event = eventEntity, EventId = eventEntity.Id, Member = _user.Member, MemberId = _user.MemberId!.Value, Status = "Confirmed", ConfirmationCode = "ABC123" });
        _context.MemberPreferences.Add(new MemberPreference { UserId = _user.Id, EmailEvents = false });
        await _context.SaveChangesAsync();

        (await _service.ProcessEventRemindersAsync()).Should().Be(1);

        _context.Notifications.Should().ContainSingle(item => item.UserId == _user.Id);
        _context.EmailOutboxMessages.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
