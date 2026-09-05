using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;

namespace HcbeApi.Tests.Services;

public sealed class ImpactAnalyticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext context = TestDbContextFactory.CreateInMemoryContext();

    [Fact]
    public async Task Dashboard_ReportsActivationAndAggregatesSmallProvinceGroups()
    {
        var members = Enumerable.Range(0, 4).Select(index => new Member
        {
            FirstName = $"Member{index}", LastName = "HCBE", Email = $"member{index}@example.com",
            Phone = "5145550100", City = index < 3 ? "Montréal" : "Halifax",
            Province = index < 3 ? "Québec" : "Nouvelle-Écosse", Interests = "communauté"
        }).ToList();
        var users = members.Select((member, index) => new User
        {
            Email = member.Email, Member = member, MemberId = member.Id, IsActive = true,
            LastLoginAtUtc = index == 0 ? DateTime.UtcNow.AddDays(-2) : null
        }).ToList();
        context.Users.AddRange(users);
        context.MemberPreferences.Add(new MemberPreference { UserId = users[0].Id, HasCompletedPreferences = true });
        var eventEntity = new Event { Title = "Rencontre", Date = DateTime.UtcNow.AddDays(2), Status = "Active" };
        context.Events.Add(eventEntity);
        context.EventRegistrations.Add(new EventRegistration { Event = eventEntity, EventId = eventEntity.Id, Member = members[0], MemberId = members[0].Id, ConfirmationCode = "IMPACT", Status = "Confirmed" });
        await context.SaveChangesAsync();

        var result = await new ImpactAnalyticsService(context).GetAsync(12);

        result.Success.Should().BeTrue();
        result.Data!.PeriodMonths.Should().Be(12);
        result.Data.Periods.Should().HaveCount(12);
        result.Data.ActivationFunnel.Should().Contain(item => item.Key == "profile" && item.Count == 4);
        result.Data.ActivationFunnel.Should().Contain(item => item.Key == "first-engagement" && item.Count == 1);
        result.Data.ProvinceBreakdown.Should().Contain(item => item.Label == "Québec" && item.Count == 3);
        result.Data.ProvinceBreakdown.Should().Contain(item => item.Key == "other" && item.Count == 1);
        ReceiptPdfRenderer.RenderImpactReport(result.Data).Should().StartWith("%PDF-"u8.ToArray());
    }

    public void Dispose() => context.Dispose();
}
