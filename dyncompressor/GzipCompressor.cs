using dyncompressor;
using System.IO.Compression;

public class GzipCompressor : ICompressionAlgorithm
{
    public string Name => "Gzip";

    public byte[] Compress(byte[] input)
    {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal))
        {
            gzip.Write(input, 0, input.Length);
        }
        return ms.ToArray();
    }

    public byte[] Decompress(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}

public class BrotliCompressor : ICompressionAlgorithm
{
    public string Name => "Brotli";

    public byte[] Compress(byte[] input)
    {
        using var ms = new MemoryStream();
        using (var brotli = new BrotliStream(ms, CompressionLevel.Optimal))
        {
            brotli.Write(input, 0, input.Length);
        }
        return ms.ToArray();
    }

    public byte[] Decompress(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var brotli = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}
