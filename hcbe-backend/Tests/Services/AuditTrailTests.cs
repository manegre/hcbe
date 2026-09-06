using System.Text.Json;
using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Tests.Services;

public sealed class AuditTrailTests
{
    [Fact]
    public async Task SaveChanges_RedactsAuthenticationDataFromAuditDetails()
    {
        await using var db = TestDbContextFactory.CreateInMemoryContext();
        var user = new User
        {
            Email = "audit-redaction@example.org",
            PasswordHash = "password-hash-must-not-appear",
            MfaSecretProtected = "mfa-secret-must-not-appear",
            MfaRecoveryCodesJson = "recovery-codes-must-not-appear",
            FirstName = "Visible"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var audit = await db.AuditLogs.SingleAsync(item => item.EntityType == nameof(User));
        using var changes = JsonDocument.Parse(audit.ChangesJson!);
        changes.RootElement.GetProperty(nameof(User.PasswordHash)).GetString().Should().Be("[REDACTED]");
        changes.RootElement.GetProperty(nameof(User.MfaSecretProtected)).GetString().Should().Be("[REDACTED]");
        changes.RootElement.GetProperty(nameof(User.MfaRecoveryCodesJson)).GetString().Should().Be("[REDACTED]");
        changes.RootElement.GetProperty(nameof(User.FirstName)).GetString().Should().Be("Visible");
        audit.ChangesJson.Should().NotContain("must-not-appear");
    }
}
