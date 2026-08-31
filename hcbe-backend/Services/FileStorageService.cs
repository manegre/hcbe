namespace HcbeApi.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> DefaultExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly HashSet<string> _allowedExtensions;
    private readonly ILogger<FileStorageService> _logger;

    public string UploadsDirectory { get; }
    public long MaxFileSizeBytes { get; }

    public FileStorageService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        MaxFileSizeBytes = configuration.GetValue("FileUpload:MaxFileSize", 10 * 1024 * 1024);

        var configuredExtensions = configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>();
        _allowedExtensions = configuredExtensions is { Length: > 0 }
            ? new HashSet<string>(configuredExtensions, StringComparer.OrdinalIgnoreCase)
            : DefaultExtensions;

        foreach (var imageExt in ImageExtensions)
        {
            _allowedExtensions.Add(imageExt);
        }

        var configuredPath = configuration["FileUpload:UploadPath"];
        UploadsDirectory = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.WebRootPath ?? "wwwroot", "uploads")
            : configuredPath;

        Directory.CreateDirectory(UploadsDirectory);
    }

    public bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension) && _allowedExtensions.Contains(extension);
    }

    public bool IsAllowedImageExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension) && ImageExtensions.Contains(extension);
    }

    public async Task<(string relativeUrl, string storedFileName)> SaveAsync(IFormFile file, string? subfolder = null)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("No file uploaded");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB");
        }

        if (!IsAllowedExtension(file.FileName))
        {
            throw new InvalidOperationException("File type is not allowed");
        }

        await FileSecurityValidator.ValidateAndGetContentTypeAsync(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var folder = FileSecurityValidator.NormalizeSubfolder(subfolder);
        var targetDirectory = Path.Combine(UploadsDirectory, folder);

        Directory.CreateDirectory(targetDirectory);
        var filePath = Path.Combine(targetDirectory, storedFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{folder}/{storedFileName}";

        return (relativeUrl, storedFileName);
    }

    public Task<bool> DeleteAsync(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        var relativePath = relativeUrl["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.GetFullPath(Path.Combine(UploadsDirectory, relativePath));
        var uploadsRoot = Path.GetFullPath(UploadsDirectory);

        if (!filePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(filePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete uploaded file {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public async Task<StoredFileContent?> ReadAsync(string? relativeUrl, CancellationToken cancellationToken = default)
    {
        var filePath = ResolveFilePath(relativeUrl);
        if (filePath == null || !File.Exists(filePath)) return null;

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return new StoredFileContent(bytes, GetContentType(Path.GetExtension(filePath)));
    }

    private string? ResolveFilePath(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return null;

        var relativePath = relativeUrl["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.GetFullPath(Path.Combine(UploadsDirectory, relativePath));
        var uploadsRoot = Path.GetFullPath(UploadsDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return filePath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) ? filePath : null;
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
