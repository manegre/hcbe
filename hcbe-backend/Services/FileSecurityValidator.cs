namespace HcbeApi.Services;

public static class FileSecurityValidator
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

    public static async Task<string> ValidateAndGetContentTypeAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidOperationException("File type is not allowed");
        }

        var header = new byte[16];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (!MatchesSignature(extension, header.AsSpan(0, bytesRead)))
        {
            throw new InvalidOperationException("File contents do not match the declared file type");
        }

        return contentType;
    }

    private static bool MatchesSignature(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".jpg" or ".jpeg" => StartsWith(header, 0xFF, 0xD8, 0xFF),
        ".png" => StartsWith(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
        ".gif" => StartsWith(header, 0x47, 0x49, 0x46, 0x38),
        ".webp" => header.Length >= 12 && StartsWith(header, 0x52, 0x49, 0x46, 0x46) &&
                   header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
        ".pdf" => StartsWith(header, 0x25, 0x50, 0x44, 0x46, 0x2D),
        ".docx" or ".xlsx" => StartsWith(header, 0x50, 0x4B, 0x03, 0x04),
        ".doc" or ".xls" => StartsWith(header, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1),
        _ => false
    };

    private static bool StartsWith(ReadOnlySpan<byte> value, params byte[] signature) =>
        value.Length >= signature.Length && value[..signature.Length].SequenceEqual(signature);

    public static string NormalizeSubfolder(string? subfolder)
    {
        if (string.IsNullOrWhiteSpace(subfolder)) return "general";
        var normalized = subfolder.Trim().ToLowerInvariant();
        if (normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new InvalidOperationException("Invalid upload folder");
        }
        return normalized;
    }
}
