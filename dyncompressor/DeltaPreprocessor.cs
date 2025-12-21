using System;

namespace dyncompressor
{
    public static class DeltaPreprocessor
    {
        public static byte[] Encode(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            byte[] delta = new byte[data.Length];
            delta[0] = data[0]; // First byte stays as-is

            for (int i = 1; i < data.Length; i++)
            {
                delta[i] = (byte)(data[i] - data[i - 1]);
            }

            return delta;
        }

        public static byte[] Decode(byte[] delta)
        {
            if (delta == null || delta.Length == 0) return delta;

            byte[] data = new byte[delta.Length];
            data[0] = delta[0];

            for (int i = 1; i < delta.Length; i++)
            {
                data[i] = (byte)(data[i - 1] + delta[i]);
            }

            return data;
        }

        // Check if data would benefit from delta encoding
        public static bool WouldBenefit(byte[] data)
        {
            if (data == null || data.Length < 100) return false;

            // Sample first 1000 bytes
            int sampleSize = Math.Min(1000, data.Length);
            int smallDeltas = 0;

            for (int i = 1; i < sampleSize; i++)
            {
                int delta = Math.Abs(data[i] - data[i - 1]);
                if (delta < 16) // Small delta
                    smallDeltas++;
            }

            // If more than 60% of deltas are small, it would benefit
            return smallDeltas > (sampleSize * 0.6);
        }
    }
}