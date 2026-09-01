using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class MemberAccountServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();

    [Fact]
    public async Task UpdateAsync_CompletesGoogleMemberProfile_WhenAllRequiredFieldsAreProvided()
    {
        var (user, _) = await CreateLinkedGoogleMemberAsync();
        var service = new MemberAccountService(_context);

        var result = await service.UpdateAsync(user.Id, CompleteProfileRequest());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Phone.Should().Be("+1 416 555 0199");
        result.Data.City.Should().Be("Toronto");
        result.Data.Province.Should().Be("Ontario");
        result.Data.Profession.Should().Be("Engineer");
        result.Data.Expertise.Should().Be("Technology");
        result.Data.Interests.Should().Be("Contribute to the HCBE community.");
    }

    [Fact]
    public async Task UpdateAsync_RejectsIncompleteFirstLoginProfile()
    {
        var (user, _) = await CreateLinkedGoogleMemberAsync();
        var service = new MemberAccountService(_context);
        var incomplete = CompleteProfileRequest() with { Phone = " " };

        var result = await service.UpdateAsync(user.Id, incomplete);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateAsync_AllowsCompleteMemberToUpdateAnOptionalField()
    {
        var (user, member) = await CreateLinkedGoogleMemberAsync();
        member.Phone = "+1 416 555 0199";
        member.City = "Toronto";
        member.Province = "Ontario";
        member.Profession = "Engineer";
        member.Expertise = "Technology";
        member.Interests = "Contribute to the HCBE community.";
        await _context.SaveChangesAsync();
        var service = new MemberAccountService(_context);

        var result = await service.UpdateAsync(
            user.Id,
            new UpdateMemberAccountRequest(null, null, null, null, null, null, null, null, "Weekends"));

        result.Success.Should().BeTrue();
        result.Data!.Availability.Should().Be("Weekends");
    }

    [Fact]
    public async Task UpdateAsync_QueuesWelcomeEmailWhenGoogleProfileBecomesComplete()
    {
        var (user, _) = await CreateLinkedGoogleMemberAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicAppUrl"] = "https://hcbe.ca",
                ["Email:ContactAddress"] = "contact@hcbe.ca"
            })
            .Build();
        var service = new MemberAccountService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration);

        var result = await service.UpdateAsync(user.Id, CompleteProfileRequest());

        result.Success.Should().BeTrue();
        var message = await _context.EmailOutboxMessages.SingleAsync();
        message.Subject.Should().Contain("Welcome");
        message.HtmlBody.Should().Contain("https://hcbe.ca/espace-membre");
    }

    private async Task<(User User, Member Member)> CreateLinkedGoogleMemberAsync()
    {
        var member = new Member
        {
            Email = $"google-{Guid.NewGuid():N}@example.org",
            FirstName = "Awa",
            LastName = "Ouédraogo"
        };
        var user = new User
        {
            Email = member.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("UnusedPassword!23"),
            FirstName = member.FirstName,
            LastName = member.LastName,
            MemberId = member.Id,
            IsAdmin = false
        };
        _context.AddRange(member, user);
        await _context.SaveChangesAsync();
        return (user, member);
    }

    private static UpdateMemberAccountRequest CompleteProfileRequest() => new(
        "Awa",
        "Ouédraogo",
        "+1 416 555 0199",
        "Toronto",
        "Ontario",
        "Engineer",
        "Technology",
        "Contribute to the HCBE community.",
        null);

    public void Dispose() => _context.Dispose();
}
