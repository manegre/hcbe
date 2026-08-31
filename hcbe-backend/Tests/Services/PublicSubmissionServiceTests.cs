using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using HcbeApi.Services;
using HcbeApi.Tests.Helpers;
using Moq;

namespace HcbeApi.Tests.Services;

public class PublicSubmissionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateInMemoryContext();
    private readonly Mock<INotificationService> _notifications = new();

    [Fact]
    public async Task SubmitAsync_WithSupportedType_ShouldPersistAndNotify()
    {
        var service = new PublicSubmissionService(_context, _notifications.Object);
        var result = await service.SubmitAsync(new CreatePublicSubmissionRequest(
            "project-contribution", "Ada", "Lovelace", "ADA@example.com", null,
            "School project", "Toronto", "I would like to help.",
            new Dictionary<string, string> { ["referenceId"] = Guid.NewGuid().ToString() }));

        result.Success.Should().BeTrue();
        var stored = _context.PublicSubmissions.Single();
        stored.Email.Should().Be("ada@example.com");
        stored.Status.Should().Be("Pending");
        stored.MetadataJson.Should().Contain("referenceId");
        _notifications.Verify(service => service.CreateNotificationAsync(
            "submission", It.IsAny<string>(), It.IsAny<string>(), stored.Id, "/admin/submissions"), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithUnsupportedType_ShouldRejectWithoutPersisting()
    {
        var service = new PublicSubmissionService(_context, _notifications.Object);
        var result = await service.SubmitAsync(new CreatePublicSubmissionRequest(
            "unknown", "Ada", "Lovelace", "ada@example.com", null, null, null,
            "Invalid type", null));

        result.Success.Should().BeFalse();
        _context.PublicSubmissions.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRecordReviewTimestamp()
    {
        var item = new PublicSubmission
        {
            Type = "contact", FirstName = "Ada", LastName = "Lovelace",
            Email = "ada@example.com", Details = "Hello"
        };
        _context.PublicSubmissions.Add(item);
        await _context.SaveChangesAsync();
        var service = new PublicSubmissionService(_context, _notifications.Object);

        var result = await service.UpdateStatusAsync(item.Id, new UpdatePublicSubmissionStatusRequest("Resolved"));

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Resolved");
        result.Data.ReviewedAt.Should().NotBeNull();
    }

    public void Dispose() => _context.Dispose();
}
