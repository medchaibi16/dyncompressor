using dyncompressor;
using dyncompressor.Engine;

public class CompressionEngine
{
    private readonly List<ICompressionAlgorithm> methods;

    public CompressionEngine()
    {
        methods = new List<ICompressionAlgorithm>
{
    new GzipCompressor(),
    new BrotliCompressor(),
    new DeflateCompressor(),
    new ZipCompressor(),
    new NoCompression(),
    new BZip2Compressor(),
    new LZMACompressor(),
    new LosslessImageCompressor(),
};

    }

    public (byte[] compressedData, string methodUsed) CompressSmart(byte[] chunk)
    {
        byte[] best = null;
        string bestMethod = "";
        long bestSize = long.MaxValue;

        foreach (var method in methods)
        {
            byte[] result = method.Compress(chunk);
            if (result.Length < bestSize)
            {
                best = result;
                bestSize = result.Length;
                bestMethod = method.Name;
            }
        }

        return (best, bestMethod);


    }
    public List<ICompressionAlgorithm> Methods => methods;

}
