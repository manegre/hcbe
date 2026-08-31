using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace HcbeApi.Tests.Services;

public class NewsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NewsService _service;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly string _tempUploadsDir;

    public NewsServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockNotificationService = new Mock<INotificationService>();
        _mockFileStorage = new Mock<IFileStorageService>();
        _tempUploadsDir = Path.Combine(Path.GetTempPath(), "hcbe-news-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempUploadsDir);

        _mockFileStorage.SetupGet(s => s.UploadsDirectory).Returns(_tempUploadsDir);
        _mockFileStorage.SetupGet(s => s.MaxFileSizeBytes).Returns(10 * 1024 * 1024);
        _mockFileStorage.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(true);
        _mockFileStorage.Setup(s => s.IsAllowedImageExtension(It.IsAny<string>())).Returns(true);
        _mockFileStorage
            .Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string?>()))
            .ReturnsAsync((IFormFile file, string? subfolder) =>
            {
                var url = $"/uploads/{subfolder ?? "news"}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
                return (url, Path.GetFileName(url));
            });
        _mockFileStorage.Setup(s => s.DeleteAsync(It.IsAny<string?>())).ReturnsAsync(true);

        _service = new NewsService(_context, _mockNotificationService.Object, _mockFileStorage.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllNews()
    {
        var news1 = new News
        {
            Id = Guid.NewGuid(),
            Title = "News 1",
            Content = "Content 1",
            Status = "published",
            PublishedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        var news2 = new News
        {
            Id = Guid.NewGuid(),
            Title = "News 2",
            Content = "Content 2",
            Status = "published",
            PublishedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.News.AddRange(news1, news2);
        await _context.SaveChangesAsync();

        var result = await _service.GetPublishedAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(2);
        result.Data.Should().OnlyContain(n => n.Attachments != null);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateNews()
    {
        var request = new CreateNewsRequest(
            "Test News",
            "Test Content",
            "Test Excerpt",
            null,
            "Author",
            "Category",
            DateTime.UtcNow,
            false,
            "published",
            "Test News EN",
            "Test Content EN",
            "Test Excerpt EN"
        );

        var result = await _service.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Test News");
        result.Data.TitleEn.Should().Be("Test News EN");
        result.Data.Content.Should().Be("Test Content");
        result.Data.ContentEn.Should().Be("Test Content EN");
        result.Data.ExcerptEn.Should().Be("Test Excerpt EN");
        result.Data.ImagePosition.Should().Be("center");
        result.Data.Attachments.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistImagePosition()
    {
        var request = new CreateNewsRequest(
            "Framed News",
            "Content",
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            false,
            "published",
            ImagePosition: "top"
        );

        var result = await _service.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.ImagePosition.Should().Be("top");

        var loaded = await _service.GetByIdForAdminAsync(result.Data.Id);
        loaded.Data!.ImagePosition.Should().Be("top");
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeInvalidImagePositionToCenter()
    {
        var request = new CreateNewsRequest(
            "Invalid Position",
            "Content",
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            ImagePosition: "diagonal"
        );

        var result = await _service.CreateAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.ImagePosition.Should().Be("center");
    }

    [Fact]
    public async Task UpdateAsync_WhenNewsExists_ShouldUpdateNews()
    {
        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Content = "Original Content",
            Status = "published",
            PublishedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.News.Add(news);
        await _context.SaveChangesAsync();

        var request = new CreateNewsRequest(
            "Updated Title",
            "Updated Content",
            "Updated Excerpt",
            null,
            "Updated Author",
            "Updated Category",
            DateTime.UtcNow,
            false,
            "draft"
        );

        var result = await _service.UpdateAsync(news.Id, request);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Updated Title");
        result.Data.Content.Should().Be("Updated Content");
    }

    [Fact]
    public async Task DeleteAsync_WhenNewsExists_ShouldDeleteNews()
    {
        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = "News to Delete",
            Content = "Content",
            Status = "published",
            PublishedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.News.Add(news);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(news.Id);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("News article deleted successfully");

        var deletedNews = await _context.News.FindAsync(news.Id);
        deletedNews.Should().BeNull();
    }

    [Fact]
    public async Task AddAttachmentAsync_ShouldPersistAttachment()
    {
        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = "With attachment",
            Content = "Content",
            Status = "published",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.News.Add(news);
        await _context.SaveChangesAsync();

        var file = CreateFormFile("brief.pdf", "application/pdf", "pdf-bytes");
        var result = await _service.AddAttachmentAsync(news.Id, file);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileName.Should().Be("brief.pdf");

        var loaded = await _service.GetByIdForAdminAsync(news.Id);
        loaded.Data!.Attachments.Should().HaveCount(1);
        loaded.Data.Attachments[0].FileName.Should().Be("brief.pdf");
    }

    [Fact]
    public async Task UploadCoverImageAsync_ShouldUpdateImageUrl()
    {
        var news = new News
        {
            Id = Guid.NewGuid(),
            Title = "Cover",
            Content = "Content",
            Status = "published",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.News.Add(news);
        await _context.SaveChangesAsync();

        var file = CreateFormFile("cover.jpg", "image/jpeg", "image-bytes");
        var result = await _service.UploadCoverImageAsync(news.Id, file);

        result.Success.Should().BeTrue();
        result.Data!.Url.Should().StartWith("/uploads/");

        var loaded = await _service.GetByIdForAdminAsync(news.Id);
        loaded.Data!.ImageUrl.Should().Be(result.Data.Url);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public void Dispose()
    {
        _context.Dispose();
        try
        {
            if (Directory.Exists(_tempUploadsDir))
            {
                Directory.Delete(_tempUploadsDir, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup failures in tests
        }
    }
}
