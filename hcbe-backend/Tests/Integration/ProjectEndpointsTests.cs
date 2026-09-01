using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HcbeApi.Tests.Integration;

public sealed class ProjectEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProject_WithEmptyDateStrings_ReturnsBadRequestInsteadOfServerError()
    {
        await AuthenticateAdminAsync();
        var payload = """
            {
              "title": "Malformed date regression",
              "location": "Toronto",
              "type": "Initiative Locale",
              "status": "Planification",
              "progress": 0,
              "description": "Regression payload",
              "budget": "1 CAD",
              "fundsRaised": "0 CAD",
              "beneficiaries": "Community",
              "startDate": "",
              "endDate": "",
              "partners": []
            }
            """;

        var response = await _client.PostAsync(
            "/api/projects",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task AuthenticateAdminAsync()
    {
        var email = $"project-admin-{Guid.NewGuid():N}@example.org";
        const string password = "AdminPassword!23";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "Project",
                LastName = "Admin",
                IsAdmin = true
            });
            await context.SaveChangesAsync();
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var payload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload!.Data!.Token);
    }

    public void Dispose() => _client.Dispose();
}
