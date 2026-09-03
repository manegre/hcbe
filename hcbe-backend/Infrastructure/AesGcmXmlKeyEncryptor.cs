using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace HcbeApi.Infrastructure;

/// <summary>
/// Encrypts the ASP.NET Data Protection key ring before it is persisted to Redis.
/// The first configured key encrypts new entries; older keys remain available for rotation.
/// </summary>
public sealed class AesGcmXmlKeyEncryptor : IXmlEncryptor
{
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("HCBE.DataProtection.v1");
    private readonly byte[] _key;
    private readonly string _keyId;

    public AesGcmXmlKeyEncryptor(string configuredKeys)
    {
        _key = DataProtectionEncryptionKeys.Parse(configuredKeys).First();
        _keyId = DataProtectionEncryptionKeys.KeyId(_key);
    }

    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        var plaintext = Encoding.UTF8.GetBytes(plaintextElement.ToString(SaveOptions.DisableFormatting));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData);

        var encrypted = new XElement("hcbeEncryptedKey",
            new XAttribute("version", "1"),
            new XAttribute("keyId", _keyId),
            new XElement("nonce", Convert.ToBase64String(nonce)),
            new XElement("tag", Convert.ToBase64String(tag)),
            new XElement("ciphertext", Convert.ToBase64String(ciphertext)));
        return new EncryptedXmlInfo(encrypted, typeof(AesGcmXmlKeyDecryptor));
    }

    internal static byte[] AssociatedDataBytes => AssociatedData;
}

public sealed class AesGcmXmlKeyDecryptor : IXmlDecryptor
{
    private readonly string? _configuredKeys;

    public AesGcmXmlKeyDecryptor()
    {
    }

    public AesGcmXmlKeyDecryptor(string configuredKeys)
    {
        _configuredKeys = configuredKeys;
    }

    public XElement Decrypt(XElement encryptedElement)
    {
        var configured = _configuredKeys ?? Environment.GetEnvironmentVariable("DataProtection__KeyEncryptionKeys")
            ?? throw new InvalidOperationException("DataProtection__KeyEncryptionKeys is required to decrypt the Data Protection key ring.");
        var keys = DataProtectionEncryptionKeys.Parse(configured);
        var keyId = encryptedElement.Attribute("keyId")?.Value;
        var orderedKeys = keys.OrderByDescending(key => DataProtectionEncryptionKeys.KeyId(key) == keyId);
        var nonce = Convert.FromBase64String(encryptedElement.Element("nonce")?.Value ?? "");
        var tag = Convert.FromBase64String(encryptedElement.Element("tag")?.Value ?? "");
        var ciphertext = Convert.FromBase64String(encryptedElement.Element("ciphertext")?.Value ?? "");

        foreach (var key in orderedKeys)
        {
            try
            {
                var plaintext = new byte[ciphertext.Length];
                using var aes = new AesGcm(key, tag.Length);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, AesGcmXmlKeyEncryptor.AssociatedDataBytes);
                return XElement.Parse(Encoding.UTF8.GetString(plaintext), LoadOptions.PreserveWhitespace);
            }
            catch (CryptographicException)
            {
                // Try retained keys during a controlled encryption-key rotation.
            }
        }

        throw new CryptographicException("The Data Protection key could not be decrypted with any configured encryption key.");
    }
}

internal static class DataProtectionEncryptionKeys
{
    internal static IReadOnlyList<byte[]> Parse(string value)
    {
        var keys = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseKey).ToList();
        if (keys.Count == 0) throw new InvalidOperationException("At least one Data Protection encryption key is required.");
        return keys;
    }

    internal static string KeyId(byte[] key) => Convert.ToHexString(SHA256.HashData(key))[..16];

    private static byte[] ParseKey(string value)
    {
        byte[] key;
        try { key = Convert.FromBase64String(value); }
        catch (FormatException exception) { throw new InvalidOperationException("Data Protection encryption keys must be Base64 encoded.", exception); }
        if (key.Length != 32) throw new InvalidOperationException("Each Data Protection encryption key must contain exactly 32 bytes.");
        return key;
    }
}
