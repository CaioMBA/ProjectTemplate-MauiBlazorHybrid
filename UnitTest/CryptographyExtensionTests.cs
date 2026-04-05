using Domain.Extensions;

namespace UnitTest;

public class CryptographyExtensionTests
{
    [Fact]
    public void EncryptAndDecrypt_ShouldRoundTrip()
    {
        const string value = "sensitive-data";

        var encrypted = value.Encrypt();
        var decrypted = encrypted.Decrypt();

        Assert.NotEqual(value, encrypted);
        Assert.Equal(value, decrypted);
    }

    [Fact]
    public void GenerateHash_ShouldBeDeterministic()
    {
        var first = CryptographyExtension.GenerateHash("value");
        var second = CryptographyExtension.GenerateHash("value");

        Assert.Equal(first, second);
        Assert.False(string.IsNullOrWhiteSpace(first));
    }
}
