using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Xunit;

namespace HcbeApi.Tests.Services;

public class MemberServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MemberService _service;

    public MemberServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _service = new MemberService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllMembers()
    {
        // Arrange
        var member1 = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow
        };
        var member2 = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Members.AddRange(member1, member2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(2);
        result.Data[0].Email.Should().Be("john@example.com"); // Ordered by CreatedAt descending (newest first)
        result.Data[1].Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoMembers_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMemberExists_ShouldReturnMember()
    {
        // Arrange
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow
        };
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(member.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(member.Id);
        result.Data.Email.Should().Be("john@example.com");
        result.Data.FirstName.Should().Be("John");
        result.Data.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetByIdAsync_WhenMemberNotFound_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Member not found");
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_WhenMemberExists_ShouldUpdateAdminStatus()
    {
        // Arrange
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateAdminStatusAsync(member.Id, true);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsAdmin.Should().BeTrue();
        
        // Verify in database
        var updatedMember = await _context.Members.FindAsync(member.Id);
        updatedMember!.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAdminStatusAsync_WhenMemberNotFound_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.UpdateAdminStatusAsync(nonExistentId, true);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Member not found");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

