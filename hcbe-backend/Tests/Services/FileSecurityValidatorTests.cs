using FluentAssertions;
using HcbeApi.Services;
using Microsoft.AspNetCore.Http;

namespace HcbeApi.Tests.Services;

public sealed class FileSecurityValidatorTests
{
    [Fact]
    public async Task ValidateAndGetContentTypeAsync_WithValidPng_ReturnsSafeContentType()
    {
        var file = CreateFile(
            "photo.png",
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);

        var result = await FileSecurityValidator.ValidateAndGetContentTypeAsync(file);

        result.Should().Be("image/png");
    }

    [Fact]
    public async Task ValidateAndGetContentTypeAsync_WithSpoofedExtension_RejectsFile()
    {
        var file = CreateFile("malware.pdf", "not a pdf"u8.ToArray());

        var action = () => FileSecurityValidator.ValidateAndGetContentTypeAsync(file);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contents do not match*");
    }

    [Theory]
    [InlineData("../private")]
    [InlineData("images/nested")]
    [InlineData("images\\nested")]
    [InlineData("images space")]
    public void NormalizeSubfolder_WithUnsafePath_RejectsFolder(string folder)
    {
        var action = () => FileSecurityValidator.NormalizeSubfolder(folder);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeSubfolder_WithSafeName_NormalizesFolder()
    {
        FileSecurityValidator.NormalizeSubfolder(" Event_Images-2026 ")
            .Should().Be("event_images-2026");
    }

    private static FormFile CreateFile(string name, byte[] contents)
    {
        var stream = new MemoryStream(contents);
        return new FormFile(stream, 0, stream.Length, "file", name);
    }
}
