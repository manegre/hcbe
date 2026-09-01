using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HcbeApi.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", "TestSecretKeyThatIsAtLeast32CharactersLong!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpirationInMinutes", "15" },
            { "JwtSettings:RefreshTokenExpirationInDays", "7" }
        });
        _configuration = configBuilder.Build();
        
        _service = new AuthService(_context, _configuration);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var token = await _service.RegisterAsync(email, password, firstName, lastName);

        // Assert
        token.Should().NotBeNull();
        token.Should().NotBeEmpty();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.IsAdmin.Should().BeFalse();
        
        // Verify password is hashed
        BCrypt.Net.BCrypt.Verify(password, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ShouldReturnNull()
    {
        // Arrange
        var email = "existing@example.com";
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var token = await _service.RegisterAsync(email, "NewPassword123!", null, null);

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var email = "test@example.com";
        var password = "TestPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var token = await _service.LoginAsync(email, password);

        // Assert
        token.Should().NotBeNull();
        token.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "TestPassword123!";

        // Act
        var token = await _service.LoginAsync(email, password);

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var email = "test@example.com";
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword);
        
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var token = await _service.LoginAsync(email, wrongPassword);

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_AfterFiveInvalidAttempts_ShouldTemporarilyLockAccount()
    {
        var user = new User
        {
            Email = "locked@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            (await _service.LoginAsync(user.Email, "WrongPassword123!")).Should().BeNull();
        }

        (await _service.LoginAsync(user.Email, "CorrectPassword123!")).Should().BeNull();
        user.LockoutEndUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ReplacesAndRevokesOriginalToken()
    {
        const string email = "session@example.com";
        const string password = "CorrectPassword123!";
        _context.Users.Add(new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });
        await _context.SaveChangesAsync();

        var first = await _service.CreateSessionAsync(email, password, "127.0.0.1");
        var second = await _service.RotateRefreshTokenAsync(first!.RefreshToken, "127.0.0.2");

        second.Should().NotBeNull();
        second!.RefreshToken.Should().NotBe(first.RefreshToken);
        (await _context.RefreshTokens.CountAsync()).Should().Be(2);
        (await _context.RefreshTokens.CountAsync(token => token.RevokedAtUtc != null)).Should().Be(1);
        (await _service.RotateRefreshTokenAsync(first.RefreshToken, "127.0.0.3")).Should().BeNull();
    }

    [Fact]
    public async Task CreateExternalSessionAsync_AllowsOnlyExistingActiveAdminWhenRequired()
    {
        var admin = new User
        {
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("UnusedPassword123!"),
            IsAdmin = true,
            IsActive = true
        };
        var member = new User
        {
            Email = "member@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("UnusedPassword123!"),
            IsAdmin = false,
            IsActive = true
        };
        _context.Users.AddRange(admin, member);
        await _context.SaveChangesAsync();

        var adminSession = await _service.CreateExternalSessionAsync(
            "ADMIN@example.com",
            "Google",
            "Admin",
            requireAdmin: true,
            "127.0.0.1");
        var memberSession = await _service.CreateExternalSessionAsync(
            member.Email,
            null,
            null,
            requireAdmin: true,
            "127.0.0.1");
        var unknownSession = await _service.CreateExternalSessionAsync(
            "unknown@example.com",
            null,
            null,
            requireAdmin: true,
            "127.0.0.1");

        adminSession.Should().NotBeNull();
        adminSession!.User.Id.Should().Be(admin.Id);
        adminSession.User.FirstName.Should().Be("Google");
        adminSession.User.LastName.Should().Be("Admin");
        adminSession.User.LastLoginAtUtc.Should().NotBeNull();
        memberSession.Should().BeNull();
        unknownSession.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrLinkMemberExternalSessionAsync_CreatesAccountForApprovedMember()
    {
        var member = new Member
        {
            Email = "approved@example.com",
            FirstName = "Awa",
            LastName = "Traore"
        };
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var session = await _service.CreateOrLinkMemberExternalSessionAsync(
            "APPROVED@example.com",
            "Awa",
            "Traore",
            "127.0.0.1");

        session.Should().NotBeNull();
        session!.User.MemberId.Should().Be(member.Id);
        session.User.IsAdmin.Should().BeFalse();
        session.User.LastLoginAtUtc.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("", session.User.PasswordHash).Should().BeFalse();
        (await _context.RefreshTokens.CountAsync(token => token.UserId == session.User.Id)).Should().Be(1);
    }

    [Fact]
    public async Task CreateOrLinkMemberExternalSessionAsync_LinksExistingAccount()
    {
        var member = new Member
        {
            Email = "member@example.com",
            FirstName = "Mariam",
            LastName = "Ouedraogo"
        };
        var user = new User
        {
            Email = member.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPassword123!")
        };
        _context.AddRange(member, user);
        await _context.SaveChangesAsync();

        var session = await _service.CreateOrLinkMemberExternalSessionAsync(
            member.Email,
            null,
            null,
            "127.0.0.1");

        session.Should().NotBeNull();
        session!.User.Id.Should().Be(user.Id);
        session.User.MemberId.Should().Be(member.Id);
    }

    [Fact]
    public async Task CreateOrLinkMemberExternalSessionAsync_CreatesMemberForNewGoogleAccount()
    {
        var session = await _service.CreateOrLinkMemberExternalSessionAsync(
            "new.member@example.com",
            "New",
            "Member",
            "127.0.0.1");

        session.Should().NotBeNull();
        session!.User.IsAdmin.Should().BeFalse();
        session.User.MemberId.Should().NotBeNull();
        var member = await _context.Members.SingleAsync();
        member.Email.Should().Be("new.member@example.com");
        member.FirstName.Should().Be("New");
        member.LastName.Should().Be("Member");
        session.User.MemberId.Should().Be(member.Id);
    }

    [Fact]
    public async Task CreateOrLinkMemberExternalSessionAsync_QueuesOnboardingEmailOnlyForFirstGoogleLogin()
    {
        var outbox = new EmailOutbox(_context);
        var templates = new EmailTemplateRenderer(_configuration);
        var service = new AuthService(_context, _configuration, outbox, templates);

        await service.CreateOrLinkMemberExternalSessionAsync(
            "onboarding@example.com", "Awa", "Traore", "127.0.0.1");
        await service.CreateOrLinkMemberExternalSessionAsync(
            "onboarding@example.com", "Awa", "Traore", "127.0.0.1");

        var message = await _context.EmailOutboxMessages.SingleAsync();
        message.Subject.Should().Contain("Complete your profile");
        message.HtmlBody.Should().Contain("/espace-membre");
    }

    [Fact]
    public async Task CreateOrLinkMemberExternalSessionAsync_RejectsInactiveAccount()
    {
        _context.Users.Add(new User
        {
            Email = "inactive@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("UnusedPassword123!"),
            IsActive = false
        });
        await _context.SaveChangesAsync();

        var session = await _service.CreateOrLinkMemberExternalSessionAsync(
            "inactive@example.com",
            "Inactive",
            "Member",
            "127.0.0.1");

        session.Should().BeNull();
        (await _context.Members.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

