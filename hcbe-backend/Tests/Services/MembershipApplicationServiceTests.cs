using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class MembershipApplicationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();

    [Fact]
    public async Task SubmitAsync_CreatesActiveNonAdminMemberAccountImmediately()
    {
        var service = new MembershipApplicationService(_context);
        var request = CreateRequest("new.member@example.org", "MemberPassword!23");

        var result = await service.SubmitAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(nameof(MembershipApplicationStatus.Approved));
        result.Data.MemberId.Should().NotBeNull();
        result.Data.ReviewedAt.Should().NotBeNull();

        var member = await _context.Members.SingleAsync();
        member.IsAdmin.Should().BeFalse();
        member.Email.Should().Be("new.member@example.org");

        var user = await _context.Users.SingleAsync();
        user.MemberId.Should().Be(member.Id);
        user.IsAdmin.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash).Should().BeTrue();

        var application = await _context.MembershipApplications.SingleAsync();
        application.PasswordHash.Should().BeNull();
        application.MemberId.Should().Be(member.Id);
    }

    [Fact]
    public async Task SubmitAsync_WithoutValidPassword_DoesNotCreatePartialRecords()
    {
        var service = new MembershipApplicationService(_context);
        var request = CreateRequest("invalid.password@example.org", "short");

        var result = await service.SubmitAsync(request);

        result.Success.Should().BeFalse();
        (await _context.Members.CountAsync()).Should().Be(0);
        (await _context.Users.CountAsync()).Should().Be(0);
        (await _context.MembershipApplications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitAsync_WhenUserEmailAlreadyExists_DoesNotCreateMember()
    {
        _context.Users.Add(new User
        {
            Email = "existing@example.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPassword!23")
        });
        await _context.SaveChangesAsync();
        var service = new MembershipApplicationService(_context);

        var result = await service.SubmitAsync(CreateRequest("EXISTING@example.org", "MemberPassword!23"));

        result.Success.Should().BeFalse();
        (await _context.Members.CountAsync()).Should().Be(0);
        (await _context.MembershipApplications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitAsync_UpgradesLegacyPendingApplicationToActiveMember()
    {
        var pending = new MembershipApplication
        {
            FirstName = "Old",
            LastName = "Applicant",
            Email = "legacy.pending@example.org",
            Status = MembershipApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };
        _context.MembershipApplications.Add(pending);
        await _context.SaveChangesAsync();
        var service = new MembershipApplicationService(_context);

        var result = await service.SubmitAsync(
            CreateRequest("LEGACY.PENDING@example.org", "MemberPassword!23"));

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(nameof(MembershipApplicationStatus.Approved));
        (await _context.Members.CountAsync()).Should().Be(1);
        (await _context.Users.CountAsync()).Should().Be(1);
        (await _context.MembershipApplications.CountAsync()).Should().Be(1);

        var upgraded = await _context.MembershipApplications.SingleAsync();
        upgraded.Id.Should().Be(pending.Id);
        upgraded.MemberId.Should().NotBeNull();
        upgraded.ReviewedAt.Should().NotBeNull();
        upgraded.FirstName.Should().Be("Awa");
    }

    [Fact]
    public async Task SubmitAsync_QueuesBrandedWelcomeEmail()
    {
        var configuration = EmailConfiguration();
        var service = new MembershipApplicationService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration);

        var result = await service.SubmitAsync(CreateRequest("welcome@example.org", "MemberPassword!23"));

        result.Success.Should().BeTrue();
        var message = await _context.EmailOutboxMessages.SingleAsync();
        message.Subject.Should().Contain("Welcome");
        message.HtmlBody.Should().Contain("https://hcbe.ca/espace-membre");
    }

    private static CreateMembershipApplicationRequest CreateRequest(string email, string password) =>
        new(
            "Awa",
            "Ouédraogo",
            email,
            "+1 416 555 0188",
            "Toronto",
            "Ontario",
            "Engineer",
            "Technology",
            "Contribute to the HCBE community.",
            password);

    private static IConfiguration EmailConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicAppUrl"] = "https://hcbe.ca",
            ["Email:ContactAddress"] = "contact@hcbe.ca"
        })
        .Build();

    public void Dispose() => _context.Dispose();
}
