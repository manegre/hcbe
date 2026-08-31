using System.Text;
using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HcbeApi.Tests.Services;

public sealed class PrivacyServiceTests
{
    [Fact]
    public async Task ExportAsync_ContainsMemberDataButNeverPasswordHash()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User
        {
            Email = "member@example.com",
            FirstName = "Test",
            PasswordHash = "highly-sensitive-password-hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var bytes = await service.ExportAsync(user.Id, CancellationToken.None);
        var json = Encoding.UTF8.GetString(bytes!);

        json.Should().Contain(user.Email);
        json.Should().NotContain(user.PasswordHash);
        json.Should().NotContain("PasswordHash");
    }

    [Fact]
    public async Task ProcessDueDeletionsAsync_AnonymizesAccountAndRevokesSessions()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User
        {
            Email = "remove-me@example.com",
            FirstName = "Remove",
            LastName = "Me",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPassword123!")
        };
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "HASH",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        context.PrivacyRequests.Add(new PrivacyRequest
        {
            UserId = user.Id,
            ExecuteAfterUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var processed = await service.ProcessDueDeletionsAsync(CancellationToken.None);

        processed.Should().Be(1);
        user.IsActive.Should().BeFalse();
        user.IsAdmin.Should().BeFalse();
        user.Email.Should().EndWith("@invalid.local");
        (await context.RefreshTokens.CountAsync()).Should().Be(0);
        var request = await context.PrivacyRequests.SingleAsync();
        request.Status.Should().Be("Completed");
        request.UserId.Should().BeNull();
        request.SubjectReference.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RequestDeletionAsync_ForAdministrator_IsRejected()
    {
        await using var context = TestDbContextFactory.CreateInMemoryContext();
        var user = new User { Email = "admin@example.com", PasswordHash = "hash", IsAdmin = true };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await CreateService(context).RequestDeletionAsync(user.Id, CancellationToken.None);

        result.Success.Should().BeFalse();
        (await context.PrivacyRequests.CountAsync()).Should().Be(0);
    }

    private static PrivacyService CreateService(HcbeApi.Data.ApplicationDbContext context)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Privacy:DeletionDelayDays"] = "30",
            ["Privacy:AuditRetentionDays"] = "730"
        }).Build();
        return new PrivacyService(context, configuration, NullLogger<PrivacyService>.Instance);
    }
}
