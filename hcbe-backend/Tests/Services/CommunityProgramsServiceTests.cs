using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;

namespace HcbeApi.Tests.Services;

public sealed class CommunityProgramsServiceTests : IDisposable
{
    private readonly ApplicationDbContext context = TestDbContextFactory.CreateInMemoryContext();
    private readonly RecordingOutbox outbox = new();

    [Fact]
    public async Task Business_RemainsPrivateUntilAdministratorApprovesIt()
    {
        var owner = await AddUser("owner@example.com");
        var created = await Service().SaveBusinessAsync(null, owner.Id,
            new("Sahel Conseils", "Sahel Consulting", "Conseil", "Accompagnement professionnel de la communauté.",
                "Professional support for the community.", "Orientation", "Guidance", owner.Email, null,
                "https://example.com", null, "Montréal", "Québec", "Canada"), default);

        created.Success.Should().BeTrue();
        (await Service().GetDirectoryAsync(null, null, null, default)).Data.Should().BeEmpty();

        await Service().ReviewBusinessAsync(created.Data!.Id,
            new(CommunityProgramStatuses.Approved, true, "Vérifié"), default);

        var directory = await Service().GetDirectoryAsync(null, null, null, default);
        directory.Data.Should().ContainSingle(item => item.IsFeatured && item.Name == "Sahel Conseils");
        outbox.Messages.Should().ContainSingle(item => item.Recipient == owner.Email);
    }

    [Fact]
    public async Task NewcomerJourney_ComputesProgressFromValidatedSteps()
    {
        var user = await AddUser("newcomer@example.com");
        var result = await Service().SaveJourneyAsync(user.Id,
            new(new DateOnly(2026, 8, 1), "Ottawa", "Ontario", "fr", ["housing"],
                ["orientation", "housing", "orientation", "unknown"], true), default);

        result.Success.Should().BeTrue();
        result.Data!.CompletedSteps.Should().BeEquivalentTo(["orientation", "housing"]);
        result.Data.ProgressPercent.Should().Be(33);
    }

    [Fact]
    public async Task FamilyMembership_EnforcesEightActivePeopleLimit()
    {
        var owner = await AddUser("family@example.com");
        await Service().SaveHouseholdAsync(owner.Id, new("Famille Ouédraogo"), default);
        for (var index = 1; index <= 8; index++)
            (await Service().AddFamilyMemberAsync(owner.Id,
                new($"Personne {index}", "Famille", null, null), default)).Success.Should().BeTrue();

        var ninth = await Service().AddFamilyMemberAsync(owner.Id,
            new("Personne 9", "Famille", null, null), default);

        ninth.Success.Should().BeFalse();
        context.FamilyHouseholdMembers.Should().HaveCount(8);
    }

    [Fact]
    public async Task CancelledAppointment_CanBeBookedAgainWithoutDuplicateRecord()
    {
        var user = await AddUser("appointment@example.com");
        var offering = new AppointmentOffering
        {
            Title = "Orientation",
            Description = "Rencontre communautaire",
            Category = "Accueil"
        };
        var slot = new AppointmentSlot
        {
            Offering = offering,
            StartsAtUtc = DateTime.UtcNow.AddDays(2),
            EndsAtUtc = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            Capacity = 1
        };
        context.AddRange(offering, slot);
        await context.SaveChangesAsync();

        var first = await Service().BookAsync(user.Id, new(slot.Id, "Premier besoin"), default);
        await Service().CancelBookingAsync(user.Id, first.Data!.Id, default);
        var second = await Service().BookAsync(user.Id, new(slot.Id, "Besoin mis à jour"), default);

        second.Success.Should().BeTrue();
        second.Data!.Status.Should().Be(CommunityProgramStatuses.Active);
        context.AppointmentBookings.Should().ContainSingle();
        context.AppointmentBookings.Single().Reason.Should().Be("Besoin mis à jour");
    }

    [Fact]
    public async Task PartnerBenefitClaim_IsIdempotentAndKeepsOneSecureCode()
    {
        var user = await AddUser("benefit@example.com");
        var partner = new Partner { Name = "Partenaire", IsActive = true };
        var benefit = new PartnerBenefit
        {
            Partner = partner,
            Title = "Rabais membre",
            Description = "Rabais réservé aux membres",
            IsActive = true
        };
        context.AddRange(partner, benefit);
        await context.SaveChangesAsync();

        var first = await Service().ClaimBenefitAsync(user.Id, benefit.Id, default);
        var second = await Service().ClaimBenefitAsync(user.Id, benefit.Id, default);

        first.Data!.RedemptionCode.Should().StartWith("HCBE-");
        second.Data!.RedemptionCode.Should().Be(first.Data.RedemptionCode);
        context.PartnerBenefitClaims.Should().ContainSingle();
    }

    [Fact]
    public async Task GrantProgram_AllowsOnlyOneApplicationPerMember()
    {
        var user = await AddUser("grant@example.com");
        var program = new GrantProgram { Title = "Bourse relève", Description = "Programme", IsActive = true };
        context.GrantPrograms.Add(program);
        await context.SaveChangesAsync();
        var request = new CreateGrantApplicationRequest(program.Id, "Awa Traoré", user.Email,
            "Je présente cette candidature afin de réaliser un projet utile à la communauté canadienne.",
            new Dictionary<string, string> { ["region"] = "Québec" }, ["https://example.com/dossier.pdf"]);

        (await Service().ApplyForGrantAsync(user.Id, request, default)).Success.Should().BeTrue();
        (await Service().ApplyForGrantAsync(user.Id, request, default)).Success.Should().BeFalse();
        context.GrantApplications.Should().ContainSingle();
    }

    private CommunityProgramsService Service() => new(context, outbox);

    private async Task<User> AddUser(string email)
    {
        var user = new User { Email = email, FirstName = "Test", LastName = "Member", IsActive = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public void Dispose() => context.Dispose();

    private sealed class RecordingOutbox : IEmailOutbox
    {
        public List<(string Recipient, string Subject)> Messages { get; } = [];
        public void Enqueue(string recipient, string subject, string htmlBody,
            string? relatedEntityType = null, Guid? relatedEntityId = null) => Messages.Add((recipient, subject));
    }
}
