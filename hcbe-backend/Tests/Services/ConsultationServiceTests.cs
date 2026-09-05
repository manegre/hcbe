using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class ConsultationServiceTests : IDisposable
{
    private readonly ApplicationDbContext context = TestDbContextFactory.CreateInMemoryContext();
    private readonly ConsultationService service;

    public ConsultationServiceTests() => service = new ConsultationService(context);

    [Fact]
    public async Task VoteAsync_AnonymousBallot_SeparatesIdentityAndRejectsDuplicate()
    {
        var user = await AddActiveMemberAsync();
        var consultation = await AddVoteAsync("Anonymous");

        var result = await service.VoteAsync(consultation.Id, user.Id, new CastConsultationVoteRequest(consultation.Options.First().Id));
        var duplicate = await service.VoteAsync(consultation.Id, user.Id, new CastConsultationVoteRequest(consultation.Options.Last().Id));

        result.Success.Should().BeTrue();
        duplicate.Success.Should().BeFalse();
        context.ConsultationParticipations.Should().ContainSingle(item => item.UserId == user.Id);
        context.ConsultationBallots.Should().ContainSingle(item => item.UserId == null);
        context.ConsultationAuditEvents.Should().ContainSingle(item => item.Action == "VoteCast" && item.UserId == null);
    }

    [Fact]
    public async Task VoteAsync_NamedBallot_PreservesVoterIdentity()
    {
        var user = await AddActiveMemberAsync();
        var consultation = await AddVoteAsync("Named");

        var result = await service.VoteAsync(consultation.Id, user.Id, new CastConsultationVoteRequest(consultation.Options.First().Id));

        result.Success.Should().BeTrue();
        context.ConsultationBallots.Single().UserId.Should().Be(user.Id);
        result.Data!.SelectedOptionId.Should().Be(consultation.Options.First().Id);
    }

    [Fact]
    public async Task PublishResultsAsync_AfterClose_ExposesAuditedQuorumResults()
    {
        var voter = await AddActiveMemberAsync();
        var admin = new User { Email = "admin@hcbe.test", PasswordHash = "hash", IsAdmin = true };
        context.Users.Add(admin);
        var consultation = await AddVoteAsync("Named", closesAtUtc: DateTime.UtcNow.AddHours(1));
        await context.SaveChangesAsync();
        await service.VoteAsync(consultation.Id, voter.Id, new CastConsultationVoteRequest(consultation.Options.First().Id));
        consultation.ClosesAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var publish = await service.PublishResultsAsync(consultation.Id, admin.Id, true);
        var publicView = await service.GetByIdAsync(consultation.Id);

        publish.Success.Should().BeTrue();
        publicView.Data!.Governance!.ResultsPublished.Should().BeTrue();
        publicView.Data.Governance.Results.Should().ContainSingle(item => item.VoteCount == 1);
        publicView.Data.Governance.QuorumReached.Should().BeTrue();
        context.ConsultationAuditEvents.Should().Contain(item => item.Action == "ResultsPublished" && item.UserId == admin.Id);
    }

    [Fact]
    public async Task VoteAsync_InactiveMember_IsNotEligible()
    {
        var member = new Member { FirstName = "Inactive", LastName = "Member", Email = "inactive@hcbe.test" };
        var user = new User { Email = member.Email, PasswordHash = "hash", Member = member, MemberId = member.Id };
        context.AddRange(member, user, new MembershipStanding { UserId = user.Id, User = user, Status = MembershipStatuses.Inactive });
        var consultation = await AddVoteAsync("Named");
        await context.SaveChangesAsync();

        var result = await service.VoteAsync(consultation.Id, user.Id, new CastConsultationVoteRequest(consultation.Options.First().Id));

        result.Success.Should().BeFalse();
        context.ConsultationBallots.Should().BeEmpty();
    }

    private async Task<User> AddActiveMemberAsync()
    {
        var member = new Member { FirstName = "Ada", LastName = "Member", Email = $"member-{Guid.NewGuid():N}@hcbe.test" };
        var user = new User { Email = member.Email, PasswordHash = "hash", Member = member, MemberId = member.Id };
        context.AddRange(member, user, new MembershipStanding { UserId = user.Id, User = user, Status = MembershipStatuses.Active });
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<Consultation> AddVoteAsync(string votingMode, DateTime? closesAtUtc = null)
    {
        var consultation = new Consultation
        {
            Title = "Priorité communautaire", Description = "Choisissez une priorité.", GovernanceType = "Vote",
            VotingMode = votingMode, EligibilityRule = "ActiveMembers", OpensAtUtc = DateTime.UtcNow.AddHours(-1),
            ClosesAtUtc = closesAtUtc ?? DateTime.UtcNow.AddHours(1), QuorumPercentage = 50, MinimumParticipation = 1,
            Options = [new ConsultationOption { Label = "Emploi" }, new ConsultationOption { Label = "Intégration", DisplayOrder = 1 }]
        };
        context.Consultations.Add(consultation);
        await context.SaveChangesAsync();
        return consultation;
    }

    public void Dispose() => context.Dispose();
}
