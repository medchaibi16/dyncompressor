using dyncompressor;
using System.IO.Compression;

public class ZipCompressor : ICompressionAlgorithm
{
    public string Name => "Zip";

    public byte[] Compress(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var zipEntry = archive.CreateEntry("data", CompressionLevel.Optimal);
            using var entryStream = zipEntry.Open();
            entryStream.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        using var entryStream = archive.Entries[0].Open();
        using var output = new MemoryStream();
        entryStream.CopyTo(output);
        return output.ToArray();
    }
}
