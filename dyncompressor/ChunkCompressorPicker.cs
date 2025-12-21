using System.IO;
using System.Linq;
using dyncompressor;
using dyncompressor.Engine;

public static class ChunkCompressorPicker
{
    // ✅ CRITICAL: Files that MUST NEVER be compressed (executables, libraries)
    private static readonly string[] ExecutableExtensions =
    {
        ".dll", ".exe", ".so", ".dylib",     // Executables and libraries
        ".pdb", ".mdb",                       // Debug symbols
        ".sys", ".ocx", ".drv"                // System drivers and controls
    };

    public static ICompressionAlgorithm SelectBest(ChunkProfile profile, string filePath)
    {
        // ✅ STEP 1: Check if this is an executable file - NEVER compress these!
        string fileExtension = Path.GetExtension(filePath ?? "").ToLowerInvariant();
        if (ExecutableExtensions.Contains(fileExtension))
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ SKIPPING COMPRESSION: {Path.GetFileName(filePath)} (executable file)");
            return new NoCompression(); // Store raw - game needs byte-perfect files
        }

        // ✅ STEP 2: NEVER use image compression for non-images
        if (profile.IsImage)
            return new LosslessImageCompressor();

        // ✅ STEP 3: For very high entropy (already compressed), store raw
        if (profile.Entropy > 7.5 && !profile.IsText)
            return new NoCompression();

        // ✅ STEP 4: For text with repeats, use BZip2
        if (profile.IsText && profile.HasRepeats)
            return new BZip2Compressor();

        // ✅ STEP 5: DEFAULT - Always use LZMA for consistency
        return new LZMACompressor();
    }
}