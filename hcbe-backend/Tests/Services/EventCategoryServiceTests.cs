using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Xunit;

namespace HcbeApi.Tests.Services;

public sealed class EventCategoryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();

    [Fact]
    public async Task CreateAsync_ShouldGenerateStableSlugAndExposeActiveCategory()
    {
        var service = new EventCategoryService(_context);

        var created = await service.CreateAsync(new CreateEventCategoryRequest(
            "Développement professionnel", "Professional development"));
        var visible = await service.GetAllAsync();

        created.Success.Should().BeTrue();
        created.Data!.Slug.Should().Be("developpement-professionnel");
        visible.Data.Should().ContainSingle(item => item.Id == created.Data.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldProtectCategoryUsedByAnEvent()
    {
        var service = new EventCategoryService(_context);
        var created = await service.CreateAsync(new CreateEventCategoryRequest("Webinaire", "Webinar", "webinar"));
        _context.Events.Add(new Event
        {
            Title = "Community webinar",
            Date = DateTime.UtcNow.AddDays(1),
            Type = created.Data!.Slug,
            Status = "Active"
        });
        await _context.SaveChangesAsync();

        var result = await service.DeleteAsync(created.Data.Id);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Deactivate");
    }

    public void Dispose() => _context.Dispose();
}
