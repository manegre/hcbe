using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HcbeApi.Tests.Integration;

public sealed class AuditEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;

    public AuditEndpointsTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAuditLogs_FiltersAndReturnsDashboardMetadata()
    {
        var email = await AuthenticateAdminAsync();
        var uniqueAction = $"ActivityTest{Guid.NewGuid():N}";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.AuditLogs.Add(new AuditLog
            {
                UserEmail = email,
                Action = uniqueAction,
                EntityType = "AuditTestEntity",
                EntityId = Guid.NewGuid().ToString(),
                ChangesJson = "{\"Status\":\"Verified\"}"
            });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/admin/audit-logs?action={uniqueAction}&entityType=AuditTestEntity&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = payload.RootElement.GetProperty("data");
        data.GetProperty("total").GetInt32().Should().Be(1);
        data.GetProperty("items")[0].GetProperty("action").GetString().Should().Be(uniqueAction);
        data.GetProperty("stats").GetProperty("retentionDays").GetInt32().Should().BeGreaterThan(0);
        data.GetProperty("filters").GetProperty("entityTypes").EnumerateArray()
            .Select(item => item.GetString()).Should().Contain("AuditTestEntity");
    }

    [Fact]
    public async Task ListAuditLogs_RequiresAdministrativeSecurityPermission()
    {
        var response = await client.GetAsync("/api/admin/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> AuthenticateAdminAsync()
    {
        var email = $"audit-admin-{Guid.NewGuid():N}@example.org";
        const string password = "AdminPassword!23";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "Audit",
                LastName = "Admin",
                IsAdmin = true
            });
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.Data!.Token);
        return email;
    }

    public void Dispose() => client.Dispose();
}
