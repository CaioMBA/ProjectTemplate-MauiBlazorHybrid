using Domain.Extensions;
using System.Text;

namespace UnitTest;

public class CompressExtensionTests
{
    [Fact]
    public void CompressAndDecompress_Bytes_ShouldRoundTrip()
    {
        var source = Encoding.UTF8.GetBytes("compress me");

        var compressed = source.Compress();
        var decompressed = compressed.Decompress();

        Assert.Equal(source, decompressed);
    }

    [Fact]
    public void Compress_String_ShouldProduceValidCompressedData()
    {
        var compressed = "abc123".Compress();
        var result = Encoding.UTF8.GetString(compressed.Decompress());

        Assert.Equal("abc123", result);
    }
}
