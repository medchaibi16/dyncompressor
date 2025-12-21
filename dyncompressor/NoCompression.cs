using dyncompressor;

public class NoCompression : ICompressionAlgorithm
{
    public string Name => "None";

    public byte[] Compress(byte[] data) => data;
    public byte[] Decompress(byte[] data) => data;
}
