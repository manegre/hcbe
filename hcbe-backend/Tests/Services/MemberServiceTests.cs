using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Helpers;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Xunit;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task ImportAsync_PreviewsThenImportsOnlyValidUniqueRows()
    {
        _context.Members.Add(new Member { FirstName = "Existing", LastName = "Member", Email = "existing@hcbe.ca" });
        await _context.SaveChangesAsync();
        var rows = new List<MemberImportRowDto>
        {
            new(2, "Awa", "Kaboré", "awa@hcbe.ca", "514 555 0101", "Montréal", "Québec", null, null, null, null, "Zone 2"),
            new(3, "Existing", "Member", "EXISTING@hcbe.ca", null, null, null, null, null, null, null, null),
            new(4, "", "Sans courriel", "incorrect", null, null, null, null, null, null, null, null)
        };

        var preview = await _service.ImportAsync(new MemberImportRequest(rows));
        preview.Data!.Preview.NewRows.Should().Be(1); preview.Data.Preview.DuplicateRows.Should().Be(1); preview.Data.Preview.InvalidRows.Should().Be(1);
        _context.Members.Should().HaveCount(1);
        var committed = await _service.ImportAsync(new MemberImportRequest(rows, true));
        committed.Data!.ImportedRows.Should().Be(1); _context.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindDuplicatesAndMergeAsync_PreservesPrimaryAndMovesRelatedData()
    {
        var primary = new Member { FirstName = "Fabrice", LastName = "Ilboudo", Email = "fabrice.one@hcbe.ca", City = "Montréal" };
        var duplicate = new Member { FirstName = "Fabrice", LastName = "Ilboudo", Email = "fabrice.two@hcbe.ca", Phone = "514-555-0102", City = "Montréal", Profession = "Ingénieur" };
        _context.Members.AddRange(primary, duplicate); await _context.SaveChangesAsync();
        _context.ServiceCases.Add(new ServiceCase { MemberId = duplicate.Id, TicketNumber = "HCBE-TEST", Category = "Test", Subject = "Test", Description = "Test" });
        await _context.SaveChangesAsync();

        (await _service.FindDuplicatesAsync()).Data.Should().ContainSingle(item => item.Primary.Id == primary.Id && item.Duplicate.Id == duplicate.Id);
        var merged = await _service.MergeAsync(primary.Id, duplicate.Id);
        merged.Success.Should().BeTrue(); merged.Data!.Profession.Should().Be("Ingénieur");
        (await _context.ServiceCases.SingleAsync()).MemberId.Should().Be(primary.Id);
        (await _context.Members.CountAsync()).Should().Be(1);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

