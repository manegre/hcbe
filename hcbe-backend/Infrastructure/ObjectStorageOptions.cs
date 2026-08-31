namespace HcbeApi.Infrastructure;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string Provider { get; set; } = "Local";
    public string? ServiceUrl { get; set; }
    public string? Region { get; set; }
    public string? BucketName { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? PublicBaseUrl { get; set; }
    public string KeyPrefix { get; set; } = "hcbe";
    public bool ForcePathStyle { get; set; } = true;

    public bool IsS3Compatible =>
        string.Equals(Provider, "S3", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Provider, "S3Compatible", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Provider, "R2", StringComparison.OrdinalIgnoreCase);

    public void Validate()
    {
        if (!IsS3Compatible) return;
        if (string.IsNullOrWhiteSpace(ServiceUrl) ||
            string.IsNullOrWhiteSpace(BucketName) ||
            string.IsNullOrWhiteSpace(AccessKey) ||
            string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException(
                "S3-compatible storage requires ServiceUrl, BucketName, AccessKey, and SecretKey.");
        }
    }
}
