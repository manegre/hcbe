using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HcbeApi.Infrastructure;
using Microsoft.Extensions.Options;

namespace HcbeApi.Services;

public sealed class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly ObjectStorageOptions _options;
    private readonly AmazonS3Client _client;
    private readonly HashSet<string> _allowedExtensions;

    public string UploadsDirectory => string.Empty;
    public long MaxFileSizeBytes { get; }

    public S3FileStorageService(IOptions<ObjectStorageOptions> options, IConfiguration configuration)
    {
        _options = options.Value;
        _options.Validate();
        MaxFileSizeBytes = configuration.GetValue("FileUpload:MaxFileSize", 10 * 1024 * 1024);
        _allowedExtensions = new HashSet<string>(
            configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            AuthenticationRegion = string.IsNullOrWhiteSpace(_options.Region) ? "auto" : _options.Region,
            ForcePathStyle = _options.ForcePathStyle
        };
        _client = new AmazonS3Client(credentials, config);
    }

    public bool IsAllowedExtension(string fileName) =>
        _allowedExtensions.Contains(Path.GetExtension(fileName));

    public bool IsAllowedImageExtension(string fileName) =>
        new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }
            .Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

    public async Task<(string relativeUrl, string storedFileName)> SaveAsync(IFormFile file, string? subfolder = null)
    {
        ValidateSizeAndExtension(file);
        var contentType = await FileSecurityValidator.ValidateAndGetContentTypeAsync(file);
        var folder = FileSecurityValidator.NormalizeSubfolder(subfolder);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var prefix = _options.KeyPrefix.Trim('/');
        var objectKey = string.IsNullOrWhiteSpace(prefix)
            ? $"{folder}/{storedFileName}"
            : $"{prefix}/{folder}/{storedFileName}";

        await using var input = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = input,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = false
        };
        request.Metadata["original-filename"] = Uri.EscapeDataString(Path.GetFileName(file.FileName));
        request.Headers.CacheControl = "public,max-age=31536000,immutable";
        await _client.PutObjectAsync(request);

        return ($"/api/storage/{objectKey}", storedFileName);
    }

    public async Task<bool> DeleteAsync(string? url)
    {
        var objectKey = ResolveObjectKey(url);
        if (objectKey == null) return false;

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        });
        return true;
    }

    public async Task<StoredFileContent?> ReadAsync(string? url, CancellationToken cancellationToken = default)
    {
        var objectKey = ResolveObjectKey(url);
        if (objectKey == null) return null;

        try
        {
            using var response = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            }, cancellationToken);
            await using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return new StoredFileContent(
                buffer.ToArray(),
                string.IsNullOrWhiteSpace(response.Headers.ContentType)
                    ? "application/octet-stream"
                    : response.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private string? ResolveObjectKey(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        const string proxyPrefix = "/api/storage/";
        string? objectKey = null;
        if (url.StartsWith(proxyPrefix, StringComparison.OrdinalIgnoreCase))
            objectKey = url[proxyPrefix.Length..];
        else if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            var publicPrefix = _options.PublicBaseUrl.TrimEnd('/') + "/";
            if (url.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
                objectKey = url[publicPrefix.Length..];
        }

        objectKey = objectKey == null ? null : Uri.UnescapeDataString(objectKey).TrimStart('/');
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.Contains("..", StringComparison.Ordinal)) return null;
        var configuredPrefix = _options.KeyPrefix.Trim('/');
        if (!string.IsNullOrWhiteSpace(configuredPrefix) &&
            !objectKey.StartsWith(configuredPrefix + "/", StringComparison.Ordinal)) return null;
        return objectKey;
    }

    private void ValidateSizeAndExtension(IFormFile file)
    {
        if (file.Length <= 0) throw new InvalidOperationException("No file uploaded");
        if (file.Length > MaxFileSizeBytes) throw new InvalidOperationException($"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB");
        if (!IsAllowedExtension(file.FileName)) throw new InvalidOperationException("File type is not allowed");
    }

    public void Dispose() => _client.Dispose();
}
