using System;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace dyncompressor
{
    public class BZip2Compressor : ICompressionAlgorithm
    {
        public string Name => "BZip2";

        public byte[] Compress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            BZip2.Compress(input, output, true, 4096); // blockSize: 4096 for better compression
            return output.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            BZip2.Decompress(input, output, true);
            return output.ToArray();
        }
    }
}
