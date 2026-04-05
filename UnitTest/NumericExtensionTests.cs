using Domain.Extensions;

namespace UnitTest;

public class NumericExtensionTests
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void IsEven_ShouldReturnExpectedResult(int value, bool expected)
    {
        Assert.Equal(expected, value.IsEven());
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void IsOdd_ShouldReturnExpectedResult(int value, bool expected)
    {
        Assert.Equal(expected, value.IsOdd());
    }

    [Fact]
    public void ToFormattedBytes_ShouldFormatExpectedUnits()
    {
        Assert.Equal("0 B", 0L.ToFormattedBytes());
        Assert.Equal("1.00 KB", 1024L.ToFormattedBytes());
        Assert.Equal("1.00 MB", (1024L * 1024L).ToFormattedBytes());
    }

    [Fact]
    public void ToFormattedBytes_NullableNull_ShouldReturnZeroBytes()
    {
        long? value = null;

        Assert.Equal("0 B", value.ToFormattedBytes());
    }

    [Fact]
    public void ToFormattedBytes_Negative_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (-1L).ToFormattedBytes());
    }
}
