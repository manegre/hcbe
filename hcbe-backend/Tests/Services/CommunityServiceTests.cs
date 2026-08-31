using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Moq;

namespace HcbeApi.Tests.Services;

public class CommunityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly CommunityService _service;
    private readonly User _mentorUser;
    private readonly User _menteeUser;

    public CommunityServiceTests()
    {
        var mentor = new Member { FirstName = "Awa", LastName = "Diallo", Email = "awa@example.test" };
        var mentee = new Member { FirstName = "Idrissa", LastName = "Ouedraogo", Email = "idrissa@example.test" };
        _mentorUser = new User { Email = mentor.Email, MemberId = mentor.Id, Member = mentor };
        _menteeUser = new User { Email = mentee.Email, MemberId = mentee.Id, Member = mentee };
        _context.Users.AddRange(_mentorUser, _menteeUser);
        _context.SaveChanges();
        _service = new CommunityService(_context, new Mock<INotificationService>().Object);
    }

    [Fact]
    public async Task Application_RequiresExplicitConsent()
    {
        var result = await _service.ApplyForMentorshipAsync(_mentorUser.Id,
            Application("Mentor") with { ConsentToShare = false });

        result.Success.Should().BeFalse();
        _context.MentorshipApplications.Should().BeEmpty();
    }

    [Fact]
    public async Task Match_BecomesActiveOnlyAfterBothMembersAccept()
    {
        var mentor = (await _service.ApplyForMentorshipAsync(_mentorUser.Id, Application("Mentor"))).Data!;
        var mentee = (await _service.ApplyForMentorshipAsync(_menteeUser.Id, Application("Mentee"))).Data!;
        await _service.ReviewApplicationAsync(mentor.Id, new ReviewMentorshipApplicationRequest("Approved", "Strong match"));
        await _service.ReviewApplicationAsync(mentee.Id, new ReviewMentorshipApplicationRequest("Approved", null));
        var match = (await _service.CreateMatchAsync(new CreateMentorshipMatchRequest(mentor.Id, mentee.Id, "Shared sector"))).Data!;

        var first = await _service.RespondToMatchAsync(_mentorUser.Id, match.Id, "Accept");
        first.Data!.Status.Should().Be("Proposed");
        first.Data.CounterpartEmail.Should().BeNull();

        var second = await _service.RespondToMatchAsync(_menteeUser.Id, match.Id, "Accept");
        second.Data!.Status.Should().Be("Active");
        second.Data.CounterpartEmail.Should().Be("awa@example.test");
    }

    [Fact]
    public async Task Directory_ReturnsOnlyOptedInProfiles()
    {
        await _service.UpsertNetworkingProfileAsync(_menteeUser.Id,
            new UpsertNetworkingProfileRequest("Technology leader", "Community-minded technology professional.", "Product strategy", "Technology", "Toronto", "Ontario", true, true));

        var visible = await _service.SearchDirectoryAsync(_mentorUser.Id, null, null);
        visible.Data.Should().ContainSingle(item => item.MemberName == "Idrissa Ouedraogo");

        await _service.UpsertNetworkingProfileAsync(_menteeUser.Id,
            new UpsertNetworkingProfileRequest("Technology leader", "Community-minded technology professional.", "Product strategy", "Technology", "Toronto", "Ontario", false, true));
        var hidden = await _service.SearchDirectoryAsync(_mentorUser.Id, null, null);
        hidden.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ConnectionRequest_SharesEmailOnlyAfterRecipientAccepts()
    {
        await _service.UpsertNetworkingProfileAsync(_menteeUser.Id,
            new UpsertNetworkingProfileRequest("Technology leader", "Community-minded technology professional.", "Product strategy", "Technology", "Toronto", "Ontario", true, true));
        var created = await _service.CreateConnectionRequestAsync(_mentorUser.Id,
            new CreateConnectionRequestRequest(_menteeUser.MemberId!.Value, "I would like to discuss our shared work."));

        created.Data!.SharedEmail.Should().BeNull();
        var accepted = await _service.RespondToConnectionRequestAsync(_menteeUser.Id, created.Data.Id,
            new RespondConnectionRequestRequest("Accepted"));
        accepted.Data!.SharedEmail.Should().Be("awa@example.test");
    }

    private static CreateMentorshipApplicationRequest Application(string role) => new(
        role, "Experienced professional committed to community service.", "Strategy and community leadership",
        "Build a practical and respectful learning relationship.", "Two hours each month", "fr", true);

    public void Dispose() => _context.Dispose();
}
