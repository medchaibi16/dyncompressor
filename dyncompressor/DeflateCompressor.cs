using dyncompressor;
using System.IO.Compression;

public class DeflateCompressor : ICompressionAlgorithm
{
    public string Name => "Deflate";

    public byte[] Compress(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var ds = new DeflateStream(ms, CompressionLevel.Optimal))
        {
            ds.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var ds = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        ds.CopyTo(output);
        return output.ToArray();
    }
}
