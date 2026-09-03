using System.Security.Cryptography;
using System.Xml.Linq;
using FluentAssertions;
using HcbeApi.Infrastructure;

namespace HcbeApi.Tests.Infrastructure;

public sealed class AesGcmXmlKeyEncryptorTests
{
    [Fact]
    public void Encrypted_key_xml_round_trips_without_exposing_plaintext()
    {
        var configuredKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var plaintext = new XElement("key", new XAttribute("id", "test"),
            new XElement("secret", "must-not-appear-in-redis"));

        var encrypted = new AesGcmXmlKeyEncryptor(configuredKey).Encrypt(plaintext).EncryptedElement;
        var decrypted = new AesGcmXmlKeyDecryptor(configuredKey).Decrypt(encrypted);

        encrypted.ToString().Should().NotContain("must-not-appear-in-redis");
        decrypted.ToString(SaveOptions.DisableFormatting)
            .Should().Be(plaintext.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void Decryptor_accepts_previous_key_during_rotation()
    {
        var oldKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var newKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var plaintext = new XElement("key", new XElement("value", "rotation-safe"));
        var encrypted = new AesGcmXmlKeyEncryptor(oldKey).Encrypt(plaintext).EncryptedElement;

        var decrypted = new AesGcmXmlKeyDecryptor($"{newKey},{oldKey}").Decrypt(encrypted);

        decrypted.ToString(SaveOptions.DisableFormatting)
            .Should().Be(plaintext.ToString(SaveOptions.DisableFormatting));
    }
}
