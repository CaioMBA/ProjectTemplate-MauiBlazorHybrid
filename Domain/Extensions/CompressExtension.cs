using System.IO.Compression;

namespace Domain.Extensions;

public static class CompressExtension
{
    public static byte[] Compress(this byte[] data, CompressionLevel level = CompressionLevel.Optimal)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var outputStream = new MemoryStream();
        using var gzipStream = new GZipStream(outputStream, level, leaveOpen: true);
        gzipStream.Write(data);
        return outputStream.ToArray();
    }

    public static byte[] Compress(this string data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var bytes = data.ToBytes();
        return bytes.Compress();
    }

    public static byte[] Decompress(this byte[] compressedData)
    {
        ArgumentNullException.ThrowIfNull(compressedData);

        using var inputStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
}
