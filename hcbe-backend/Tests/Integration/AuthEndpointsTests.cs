using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Tests.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using HcbeApi.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcbeApi.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_IsNotExposed_ShouldReturnNotFound()
    {
        // Arrange
        var request = new RegisterRequest(
            $"test{Guid.NewGuid()}@example.com",
            "TestPassword123!",
            "John",
            "Doe"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        // Arrange - provision a user through the trusted data layer.
        var email = $"login{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "John",
                LastName = "Doe"
            });
            await context.SaveChangesAsync();
        }

        // Act - Login
        var loginRequest = new LoginRequest(email, password);
        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Email.Should().Be(email);
        response.Headers.GetValues("Set-Cookie").Single().Should().ContainEquivalentOf("HttpOnly");

        var refreshResponse = await _client.PostAsync("/api/auth/refresh", content: null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        refreshed!.Data!.Token.Should().NotBe(apiResponse.Data.Token);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await verificationContext.RefreshTokens.CountAsync(token => token.UserId == apiResponse.Data.User.Id))
            .Should().Be(2);
        (await verificationContext.RefreshTokens.CountAsync(token =>
            token.UserId == apiResponse.Data.User.Id && token.RevokedAtUtc != null)).Should().Be(1);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "WrongPassword123!");
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoogleAdminLogin_WhenNotConfigured_ShouldReturnServiceUnavailable()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google/admin",
            new GoogleLoginRequest("untrusted-token"));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GoogleMemberLogin_WhenNotConfigured_ShouldReturnServiceUnavailable()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google/member",
            new GoogleLoginRequest("untrusted-token"));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

