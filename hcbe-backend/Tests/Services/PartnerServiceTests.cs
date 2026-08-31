using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;

namespace HcbeApi.Tests.Services;

public class PartnerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();

    [Fact]
    public async Task CreateAsync_ShouldNormalizeAndPersistPartner()
    {
        var service = new PartnerService(_context);

        var result = await service.CreateAsync(new CreatePartnerRequest(
            "  Burkina Innovation  ",
            "Burkina Innovation",
            "  Partenaire technologique  ",
            null,
            "/uploads/partners/logo.png",
            "https://example.com",
            "Logo Burkina Innovation",
            null,
            true,
            true,
            2));

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Burkina Innovation");
        result.Data.Description.Should().Be("Partenaire technologique");
        _context.Partners.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAllAsync_ShouldHideInactiveAndRespectDisplayOrder()
    {
        _context.Partners.AddRange(
            new Partner { Name = "Second", DisplayOrder = 2, IsActive = true },
            new Partner { Name = "Hidden", DisplayOrder = 0, IsActive = false },
            new Partner { Name = "First", DisplayOrder = 1, IsActive = true });
        await _context.SaveChangesAsync();
        var service = new PartnerService(_context);

        var result = await service.GetAllAsync();

        result.Data!.Select(item => item.Name).Should().ContainInOrder("First", "Second");
        result.Data.Should().NotContain(item => item.Name == "Hidden");
    }

    [Fact]
    public async Task ReorderAsync_ShouldPersistRequestedOrder()
    {
        var first = new Partner { Name = "First", DisplayOrder = 0 };
        var second = new Partner { Name = "Second", DisplayOrder = 1 };
        _context.Partners.AddRange(first, second);
        await _context.SaveChangesAsync();
        var service = new PartnerService(_context);

        var result = await service.ReorderAsync(new ReorderPartnersRequest(new List<Guid> { second.Id, first.Id }));

        result.Success.Should().BeTrue();
        result.Data!.Select(item => item.Id).Should().ContainInOrder(second.Id, first.Id);
        first.DisplayOrder.Should().Be(1);
        second.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ShouldReject()
    {
        _context.Partners.Add(new Partner { Name = "Existing Partner" });
        await _context.SaveChangesAsync();
        var service = new PartnerService(_context);

        var result = await service.CreateAsync(new CreatePartnerRequest("existing partner"));

        result.Success.Should().BeFalse();
        _context.Partners.Should().ContainSingle();
    }

    public void Dispose() => _context.Dispose();
}
