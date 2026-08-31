using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace HcbeApi.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventService _service;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IFileStorageService> _mockFileStorage;

    public EventServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockNotificationService = new Mock<INotificationService>();
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockFileStorage.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(true);
        _mockFileStorage.Setup(s => s.IsAllowedImageExtension(It.IsAny<string>())).Returns(true);
        _mockFileStorage
            .Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string?>()))
            .ReturnsAsync((IFormFile file, string? folder) =>
                ($"/uploads/{folder ?? "events"}/{file.FileName}", file.FileName));
        _service = new EventService(_context, _mockNotificationService.Object, _mockFileStorage.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEvents()
    {
        // Arrange
        var event1 = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event 1",
            Date = DateTime.UtcNow.AddDays(1),
            Status = "À venir",
            CreatedAt = DateTime.UtcNow
        };
        var event2 = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event 2",
            Date = DateTime.UtcNow.AddDays(2),
            Status = "À venir",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Events.AddRange(event1, event2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(2);
        result.Data[0].Title.Should().Be("Event 1"); // Ordered by CreatedAt descending (newest first)
        result.Data[1].Title.Should().Be("Event 2");
    }

    [Fact]
    public async Task PublicQueries_ShouldHideDraftAndCancelledEvents()
    {
        var published = new Event { Title = "Published", Date = DateTime.UtcNow.AddDays(1), Status = "À venir" };
        var draft = new Event { Title = "Draft", Date = DateTime.UtcNow.AddDays(2), Status = "Draft" };
        var cancelled = new Event { Title = "Cancelled", Date = DateTime.UtcNow.AddDays(3), Status = "Annulé" };
        _context.Events.AddRange(published, draft, cancelled);
        await _context.SaveChangesAsync();

        var publicList = await _service.GetAllAsync();
        var adminList = await _service.GetAllForAdminAsync();
        var publicDraft = await _service.GetByIdAsync(draft.Id);
        var adminDraft = await _service.GetByIdForAdminAsync(draft.Id);

        publicList.Data!.Select(item => item.Title).Should().ContainSingle().Which.Should().Be("Published");
        adminList.Data.Should().HaveCount(3);
        publicDraft.Success.Should().BeFalse();
        adminDraft.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ShouldReturnEvent()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Test Description",
            Date = DateTime.UtcNow.AddDays(1),
            Location = "Test Location",
            Status = "À venir",
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(eventEntity.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(eventEntity.Id);
        result.Data.Title.Should().Be("Test Event");
        result.Data.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEvent()
    {
        // Arrange
        var request = new CreateEventRequest(
            "New Event",
            "Event Description",
            DateTime.UtcNow.AddDays(1),
            "Location",
            "Workshop",
            "Zone 1",
            50,
            DateTime.UtcNow.AddDays(1),
            null,
            null,
            "À venir",
            "New Event EN",
            "Event Description EN",
            "Location EN"
        );

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("New Event");
        result.Data.TitleEn.Should().Be("New Event EN");
        result.Data.Description.Should().Be("Event Description");
        result.Data.DescriptionEn.Should().Be("Event Description EN");
        result.Data.LocationEn.Should().Be("Location EN");
        result.Data.Capacity.Should().Be(50);

        // Verify in database
        var createdEvent = await _context.Events.FindAsync(result.Data.Id);
        createdEvent.Should().NotBeNull();
        createdEvent!.Title.Should().Be("New Event");
    }

    [Fact]
    public async Task UpdateAsync_WhenEventExists_ShouldUpdateEvent()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Date = DateTime.UtcNow.AddDays(1),
            Status = "À venir",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var request = new UpdateEventRequest(
            "Updated Title",
            "Updated Description",
            DateTime.UtcNow.AddDays(2),
            "Updated Location",
            null,
            null,
            null,
            null,
            null,
            null,
            null // Status
        );

        // Act
        var result = await _service.UpdateAsync(eventEntity.Id, request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Updated Title");
        result.Data.Description.Should().Be("Updated Description");
        result.Data.Location.Should().Be("Updated Location");

        // Verify in database
        var updatedEvent = await _context.Events.FindAsync(eventEntity.Id);
        updatedEvent!.Title.Should().Be("Updated Title");
        updatedEvent.UpdatedAt.Should().BeOnOrAfter(eventEntity.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WhenEventNotFound_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateEventRequest(
            "Title",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null // Status
        );

        // Act
        var result = await _service.UpdateAsync(nonExistentId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Be("Event not found");
    }

    [Fact]
    public async Task DeleteAsync_WhenEventExists_ShouldDeleteEvent()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Event to Delete",
            Date = DateTime.UtcNow.AddDays(1),
            Status = "À venir",
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(eventEntity.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Event deleted successfully");

        // Verify in database
        var deletedEvent = await _context.Events.FindAsync(eventEntity.Id);
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenEventNotFound_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Event not found");
    }

    [Fact]
    public async Task AddVideoAsync_WithYoutubeUrl_ShouldPersistMedia()
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Past Event",
            Date = DateTime.UtcNow.AddDays(-10),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var result = await _service.AddVideoAsync(
            eventEntity.Id,
            new AddEventVideoRequest("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "Highlight", "English highlight"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.MediaType.Should().Be("video");
        result.Data.Caption.Should().Be("Highlight");
        result.Data.CaptionEn.Should().Be("English highlight");

        var byId = await _service.GetByIdAsync(eventEntity.Id);
        byId.Data!.Media.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddVideoAsync_WithInvalidUrl_ShouldReturnError()
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Past Event",
            Date = DateTime.UtcNow.AddDays(-10),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var result = await _service.AddVideoAsync(
            eventEntity.Id,
            new AddEventVideoRequest("https://example.com/video", null));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("YouTube or Vimeo");
    }

    [Fact]
    public async Task AddAttachmentAsync_ShouldPersistAttachment()
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = "With attachment",
            Date = DateTime.UtcNow.AddDays(1),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var content = new MemoryStream("pdf-bytes"u8.ToArray());
        IFormFile file = new FormFile(content, 0, content.Length, "file", "agenda.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await _service.AddAttachmentAsync(eventEntity.Id, file);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileName.Should().Be("agenda.pdf");

        var byId = await _service.GetByIdAsync(eventEntity.Id);
        byId.Data!.Attachments.Should().HaveCount(1);
        byId.Data.Attachments[0].FileName.Should().Be("agenda.pdf");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

