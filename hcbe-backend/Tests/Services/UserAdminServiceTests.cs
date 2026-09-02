using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class UserAdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();

    private UserAdminService CreateService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicAppUrl"] = "https://hcbe.ca",
            ["Email:ContactAddress"] = "contact@hcbe.ca"
        }).Build();
        return new UserAdminService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration);
    }

    [Fact]
    public async Task CreateAdminUserAsync_CreatesInvitationMemberAccessAndWelcomeEmail()
    {
        const string temporaryPassword = "TempAccess!2026Secure";
        var service = CreateService();

        var response = await service.CreateAdminUserAsync(new CreateAdminUserRequest(
            "new.admin@example.com", temporaryPassword, "Awa", "Traore"));

        response.Success.Should().BeTrue();
        var user = await _context.Users.SingleAsync();
        user.IsAdmin.Should().BeTrue();
        user.MustChangePassword.Should().BeTrue();
        user.MemberId.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify(temporaryPassword, user.PasswordHash).Should().BeTrue();

        var member = await _context.Members.SingleAsync();
        member.Id.Should().Be(user.MemberId!.Value);
        member.IsAdmin.Should().BeTrue();
        member.Email.Should().Be(user.Email);

        var welcome = await _context.EmailOutboxMessages.SingleAsync();
        welcome.Recipient.Should().Be(user.Email);
        welcome.Subject.Should().Contain("Administrator access");
        welcome.HtmlBody.Should().Contain(temporaryPassword);
        welcome.HtmlBody.Should().Contain("https://hcbe.ca/admin/login");
    }

    [Fact]
    public async Task PromoteMemberAsync_PromotesExistingGoogleAccountAndQueuesNotification()
    {
        var member = new Member
        {
            FirstName = "Fabrice",
            LastName = "Ilboudo",
            Email = "ilboudofabrice@gmail.com"
        };
        var user = new User
        {
            Email = "ilboudofabrice@gmail.com",
            FirstName = "Fabrice",
            LastName = "Ilboudo",
            MemberId = member.Id,
            PasswordHash = string.Empty,
            IsAdmin = false
        };
        _context.AddRange(member, user);
        await _context.SaveChangesAsync();

        var response = await CreateService().PromoteMemberAsync(member.Id);

        response.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(user.Id);
        member.IsAdmin.Should().BeTrue();
        user.IsAdmin.Should().BeTrue();
        user.MustChangePassword.Should().BeFalse();
        var notification = await _context.EmailOutboxMessages.SingleAsync();
        notification.Recipient.Should().Be(user.Email);
        notification.Subject.Should().Contain("Administrator access granted");
        notification.HtmlBody.Should().Contain("https://hcbe.ca/admin/login");
    }

    [Fact]
    public async Task PromoteMemberAsync_CreatesTemporaryAccountWhenMemberHasNoUser()
    {
        var member = new Member
        {
            FirstName = "Awa",
            LastName = "Traore",
            Email = "awa.member@example.com"
        };
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var response = await CreateService().PromoteMemberAsync(member.Id);

        response.Success.Should().BeTrue();
        member.IsAdmin.Should().BeTrue();
        var user = await _context.Users.SingleAsync();
        user.IsAdmin.Should().BeTrue();
        user.MemberId.Should().Be(member.Id);
        user.MustChangePassword.Should().BeTrue();
        (await _context.EmailOutboxMessages.SingleAsync()).Subject.Should().Contain("Administrator access");
    }

    public void Dispose() => _context.Dispose();
}
