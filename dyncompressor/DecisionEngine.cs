using System;
using System.IO;
using System.Linq;

namespace dyncompressor
{
    public enum CompressionDecision
    {
        Store,          // write raw bytes (no compression)
        FullCompress,   // compress entire file with chosen algorithm
        ChunkCompress,  // split file and compress chunks
        ImageSpecial,   // use image-specialized compressors
        BinaryTransform // reserved / treat like chunk for now
    }

    public static class DecisionEngine
    {
        // thresholds & config
        private const double ENTROPY_HIGH = 7.2;   // very high entropy -> likely compressed
        private const double ENTROPY_LOW = 4.0;    // low entropy -> compressible
        private const int SMALL_FILE_BYTES = 128 * 1024; // 128KB treat as small file

        // Decide for a file path. We need the available compressors list to run sample tests.
        // dynamicCompressorCandidates: array of compressors to test (e.g. LZMA, Brotli, Gzip,...)
        public static CompressionDecision Decide(string filePath, ICompressionAlgorithm[] dynamicCompressorCandidates, out ICompressionAlgorithm chosenAlgorithm)
        {
            chosenAlgorithm = null;
            try
            {
                // Quick checks
                if (FileClassifier.IsProbablyAlreadyCompressed(filePath))
                {
                    return CompressionDecision.Store;
                }

                // Sample file and compute entropy
                byte[] sample = FileClassifier.SampleFile(filePath, 32 * 1024); // 32KB sections
                double entropy = FileClassifier.ShannonEntropy(sample);

                // If entropy very high -> store raw
                if (entropy >= ENTROPY_HIGH)
                    return CompressionDecision.Store;

                // If small file and estimatable
                FileInfo fi = new FileInfo(filePath);
                bool isSmall = fi.Length <= SMALL_FILE_BYTES;

                // image special case by extension
                var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? "";
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".webp" || ext == ".bmp" || ext == ".tga")
                {
                    // let the image compressor handle whether compressible or not
                    chosenAlgorithm = dynamicCompressorCandidates?.FirstOrDefault(c => c.Name == "LosslessImageCompressor");
                    return CompressionDecision.ImageSpecial;
                }

                // Quick sample test: does any algorithm give meaningful compression on the sample?
                var best = PreCompressionTester.ChooseBestCompressorBySample(sample, dynamicCompressorCandidates, requiredRatio: 0.99);
                if (best != null)
                {
                    chosenAlgorithm = best;
                    // if file small, do full-file compression; for larger files prefer chunking to keep memory low
                    return isSmall ? CompressionDecision.FullCompress : CompressionDecision.ChunkCompress;
                }

                // fallback: if entropy low-ish, attempt full-file with strongest algorithm (LZMA) using small test
                var lzma = dynamicCompressorCandidates?.FirstOrDefault(c => c.Name.ToLower().Contains("lzma") || c.Name.ToLower().Contains("xz"));
                if (lzma != null)
                {
                    double ratio = PreCompressionTester.TestCompressionRatio(lzma, sample);
                    if (ratio < 0.995)
                    {
                        chosenAlgorithm = lzma;
                        return isSmall ? CompressionDecision.FullCompress : CompressionDecision.ChunkCompress;
                    }
                }

                // final conservative fallback
                return CompressionDecision.Store;
            }
            catch
            {
                chosenAlgorithm = null;
                return CompressionDecision.Store;
            }
        }
    }
}
