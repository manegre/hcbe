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

    [Fact]
    public async Task CreateAdminUserAsync_CreatesInvitationMemberAccessAndWelcomeEmail()
    {
        const string temporaryPassword = "TempAccess!2026Secure";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicAppUrl"] = "https://hcbe.ca",
            ["Email:ContactAddress"] = "contact@hcbe.ca"
        }).Build();
        var service = new UserAdminService(
            _context,
            new EmailOutbox(_context),
            new EmailTemplateRenderer(configuration),
            configuration);

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

    public void Dispose() => _context.Dispose();
}
