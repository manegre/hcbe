using System.Text.RegularExpressions;
using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HcbeApi.Tests.Services;

public sealed class PasswordResetServiceTests : IDisposable
{
    private readonly HcbeApi.Data.ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicAppUrl"] = "https://hcbe.ca",
            ["Email:ContactAddress"] = "contact@hcbe.ca"
        })
        .Build();

    [Fact]
    public async Task ResetLifecycle_QueuesBrandedResetAndPasswordChangedEmails()
    {
        var user = new User
        {
            Email = "member@example.org",
            FirstName = "Awa",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword!23")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var service = CreateService();

        var request = await service.RequestAsync(new RequestPasswordResetRequest(user.Email), CancellationToken.None);

        request.Success.Should().BeTrue();
        var resetEmail = await _context.EmailOutboxMessages.SingleAsync();
        resetEmail.Subject.Should().Contain("Password reset");
        resetEmail.HtmlBody.Should().Contain("https://hcbe.ca/espace-membre?resetToken=");
        var token = Regex.Match(resetEmail.HtmlBody, "resetToken=([A-F0-9]+)").Groups[1].Value;
        token.Should().NotBeEmpty();

        var confirmation = await service.ConfirmAsync(
            new ConfirmPasswordResetRequest(token, "NewPassword!23"),
            CancellationToken.None);

        confirmation.Success.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("NewPassword!23", user.PasswordHash).Should().BeTrue();
        var messages = await _context.EmailOutboxMessages.OrderBy(item => item.CreatedAtUtc).ToListAsync();
        messages.Should().HaveCount(2);
        messages[1].Subject.Should().Contain("Password changed");
    }

    private PasswordResetService CreateService()
    {
        var templates = new EmailTemplateRenderer(_configuration);
        return new PasswordResetService(_context, new EmailOutbox(_context), templates, _configuration);
    }

    public void Dispose() => _context.Dispose();
}
