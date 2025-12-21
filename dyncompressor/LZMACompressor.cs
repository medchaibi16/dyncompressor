using SevenZip;
using System;
using System.IO;

namespace dyncompressor
{
    public class LZMACompressor : ICompressionAlgorithm
    {
        public string Name => "LZMA";

        public byte[] Compress(byte[] input)
        {
            try
            {
                using var inputStream = new MemoryStream(input);
                using var outputStream = new MemoryStream();

                var compressor = new SevenZipCompressor();
                compressor.CompressionMethod = CompressionMethod.Lzma;
                compressor.CompressionLevel = CompressionLevel.Normal;

                // Use stream-based compression (no temp files)
                compressor.CompressStream(inputStream, outputStream);

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LZMA compression failed: {ex.Message}");
                return input; // Return original on failure
            }
        }

        public byte[] Decompress(byte[] input)
        {
            try
            {
                using var inputStream = new MemoryStream(input);
                using var outputStream = new MemoryStream();

                using (var extractor = new SevenZipExtractor(inputStream))
                {
                    // Extract to memory stream
                    extractor.ExtractFile(0, outputStream);
                }

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LZMA decompression failed: {ex.Message}");
                throw new Exception($"LZMA decompression failed: {ex.Message}", ex);
            }
        }
    }
}