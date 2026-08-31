using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using Xunit;

namespace HcbeApi.Tests.Integration;

public class MemberEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MemberEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMembers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMembers_AsAdmin_ShouldReturnOk()
    {
        await AuthenticateAsAdminAsync();
        var response = await _client.GetAsync("/api/members");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<MemberDto>>>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetMembers_AsAuthenticatedMember_ShouldReturnForbidden()
    {
        var email = $"member-{Guid.NewGuid()}@example.com";
        const string password = "TestPassword123!";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsAdmin = false
            });
            await context.SaveChangesAsync();
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var payload = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.Data!.Token);

        var response = await _client.GetAsync("/api/members");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMember_WhenMemberExists_ShouldReturnOk()
    {
        // Arrange - Create a member first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        context.Members.Add(member);
        await context.SaveChangesAsync();
        await AuthenticateAsAdminAsync();

        // Act
        var response = await _client.GetAsync($"/api/members/{member.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<MemberDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetMember_WhenMemberNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        await AuthenticateAsAdminAsync();

        // Act
        var response = await _client.GetAsync($"/api/members/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var email = $"admin-{Guid.NewGuid()}@example.com";
        const string password = "TestPassword123!";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "Test",
                LastName = "Admin",
                IsAdmin = true
            });
            await context.SaveChangesAsync();
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var payload = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        payload!.Data!.Token.Should().NotBeNullOrEmpty();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Data.Token);
    }
}

