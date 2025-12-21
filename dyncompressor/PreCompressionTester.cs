using System;
using System.IO;
using System.Linq;

namespace dyncompressor
{
    public static class PreCompressionTester
    {
        // Try compressing 'sample' with compressor; return compression ratio (compressed / original).
        // If compressor or sample fails, return double.PositiveInfinity (meaning "do not trust").
        public static double TestCompressionRatio(ICompressionAlgorithm compressor, byte[] sample)
        {
            try
            {
                if (sample == null || sample.Length == 0) return double.PositiveInfinity;
                byte[] compressed = compressor.Compress(sample);
                if (compressed == null) return double.PositiveInfinity;
                // ratio < 1 means it compressed smaller
                return (double)compressed.Length / (double)sample.Length;
            }
            catch
            {
                return double.PositiveInfinity;
            }
        }

        // run a quick multi-algo test: returns best compressor or null if none saved size by threshold
        public static ICompressionAlgorithm ChooseBestCompressorBySample(byte[] sample, ICompressionAlgorithm[] candidates, double requiredRatio = 0.98)
        {
            if (sample == null || sample.Length == 0 || candidates == null || candidates.Length == 0) return null;

            ICompressionAlgorithm best = null;
            double bestRatio = double.PositiveInfinity;

            foreach (var c in candidates)
            {
                double ratio = TestCompressionRatio(c, sample);
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    best = c;
                }
            }

            // only accept if bestRatio is a meaningful saving (e.g. < requiredRatio)
            return (bestRatio < requiredRatio) ? best : null;
        }
    }
}
