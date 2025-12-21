using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dyncompressor.Engine
{
    public class CompressionResult
    {
        public string MethodName { get; set; }
        public byte[] CompressedData { get; set; }
    }

    public class DynamicCompressor
    {
        private readonly List<ICompressionAlgorithm> _compressors;

        public DynamicCompressor(IEnumerable<ICompressionAlgorithm> compressors)
        {
            _compressors = compressors.ToList();
        }

        public CompressionResult CompressChunk(byte[] chunk)
        {
            CompressionResult bestResult = null;

            foreach (var compressor in _compressors)
            {
                try
                {
                    byte[] compressed = compressor.Compress(chunk);
                    if (bestResult == null || compressed.Length < bestResult.CompressedData.Length)
                    {
                        bestResult = new CompressionResult
                        {
                            MethodName = compressor.Name,
                            CompressedData = compressed
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Compression failed with {compressor.Name}: {ex.Message}");
                }
            }

            return bestResult;
        }
    }
}
