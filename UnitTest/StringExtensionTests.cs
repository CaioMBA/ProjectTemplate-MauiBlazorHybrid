using Domain.Extensions;

namespace UnitTest;

public class StringExtensionTests
{
    [Fact]
    public void Capitalize_ShouldUppercaseFirstCharacter()
    {
        var result = "hello".Capitalize();

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToBase64_And_FromBase64_ShouldRoundTrip()
    {
        const string value = "test-value";

        var encoded = value.ToBase64();
        var decoded = encoded.FromBase64();

        Assert.Equal(value, decoded);
        Assert.True(encoded.IsBase64String());
    }

    [Theory]
    [InlineData("12", true, 12)]
    [InlineData("ABC", false, 0)]
    public void ToInteger_ShouldParseExpectedValues(string value, bool expectedParsed, int expectedNumber)
    {
        var parsed = value.ToInteger(out var result);

        Assert.Equal(expectedParsed, parsed);
        Assert.Equal(expectedNumber, result);
    }

    [Fact]
    public void ToSha256_ShouldReturnExpectedHash()
    {
        var result = "abc".ToSHA256();

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", result);
    }

    [Theory]
    [InlineData("light", "Light")]
    [InlineData("dark", "Dark")]
    [InlineData("system", "Unspecified")]
    public void ToAppTheme_ShouldMapExpectedTheme(string value, string expectedTheme)
    {
        var result = value.ToAppTheme();

        Assert.Equal(expectedTheme, result.ToString());
    }
}
