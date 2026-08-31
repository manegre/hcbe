namespace HcbeApi.Services;

public interface IFileStorageService
{
    string UploadsDirectory { get; }
    Task<(string relativeUrl, string storedFileName)> SaveAsync(IFormFile file, string? subfolder = null);
    Task<StoredFileContent?> ReadAsync(string? url, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string? url);
    bool IsAllowedExtension(string fileName);
    bool IsAllowedImageExtension(string fileName);
    long MaxFileSizeBytes { get; }
}

public sealed record StoredFileContent(byte[] Bytes, string ContentType);
