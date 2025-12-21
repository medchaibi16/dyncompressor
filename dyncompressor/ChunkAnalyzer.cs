using dyncompressor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ChunkAnalyzer
{
    public static ChunkProfile Analyze(byte[] chunk)
    {
        bool isText = chunk.All(b => b == 0x0A || b == 0x0D || (b >= 32 && b <= 126));
        bool hasRepeats = DetectRepetition(chunk);
        bool isImage = IsImageData(chunk);

        double entropy = CalculateEntropy(chunk); // 🔹 add this line

        return new ChunkProfile
        {
            IsImage = isImage,
            IsText = isText,
            HasRepeats = hasRepeats,
            Entropy = entropy
        };
    }

    public static double CalculateEntropy(byte[] data)
    {
        if (data == null || data.Length == 0) return 0;

        int[] counts = new int[256];
        foreach (var b in data) counts[b]++;

        double entropy = 0.0;
        double length = data.Length;

        for (int i = 0; i < 256; i++)
        {
            if (counts[i] == 0) continue;
            double p = counts[i] / length;
            entropy -= p * Math.Log2(p);
        }

        return entropy;
    }

    // ❌ Remove this entire method because it depends on EntropyScanner
    // public static bool IsHighlyCompressible(byte[] bytes) { ... }

    private static bool DetectRepetition(byte[] chunk)
    {
        var set = new HashSet<byte>(chunk);
        return set.Count < chunk.Length * 0.8;
    }

    public static bool IsImageData(byte[] data)
    {
        if (data.Length < 4) return false;

        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return true;

        if (data[0] == 0xFF && data[1] == 0xD8)
            return true;

        if (data[0] == 0x42 && data[1] == 0x4D)
            return true;

        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
            return true;

        if (data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00)
            return true;

        if (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A)
            return true;

        return false;
    }
}
