using System;
using System.IO;

namespace dyncompressor
{
    public enum CompressionStrategy
    {
        Chunk,
        FullFile,
        FormatOptimized
    }

    public static class CompressionStrategySelector
    {
        // Tunable thresholds
        private const long LARGE_FILE_THRESHOLD_BYTES = 200 * 1024 * 1024; // 200 MB
        private const int MANY_FILES_THRESHOLD = 200;
        private const long SMALL_FILE_BYTES = 32 * 1024; // 32 KB

        // Decide strategy for a single file path
        public static CompressionStrategy DecideForFile(string filePath)
        {
            try
            {
                var fi = new FileInfo(filePath);
                if (!fi.Exists) return CompressionStrategy.Chunk;

                long size = fi.Length;
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                // If image/audio special ext -> format-optimized
                if (IsFormatOptimizedExtension(ext))
                    return CompressionStrategy.FormatOptimized;

                // Large single files -> chunk
                if (size >= LARGE_FILE_THRESHOLD_BYTES)
                    return CompressionStrategy.Chunk;

                // medium / small files -> try full-file (often better for small files)
                return CompressionStrategy.FullFile;
            }
            catch
            {
                return CompressionStrategy.Chunk;
            }
        }

        // Decide strategy for a folder: look at distribution
        public static CompressionStrategy DecideForFolder(string folderPath)
        {
            try
            {
                var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                if (files.Length == 0) return CompressionStrategy.Chunk;

                int count = files.Length;
                long total = 0;
                int smallFiles = 0;
                int formatOptimizedCount = 0;

                foreach (var f in files)
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        total += fi.Length;
                        if (fi.Length <= SMALL_FILE_BYTES) smallFiles++;
                        if (IsFormatOptimizedExtension(Path.GetExtension(f).ToLowerInvariant())) formatOptimizedCount++;
                    }
                    catch { /* ignore inaccessible files */ }
                }

                double avg = (double)total / Math.Max(1, count);

                // Many small files -> solid / full-file approach (pack together)
                if (count >= MANY_FILES_THRESHOLD || smallFiles > (count / 2))
                    return CompressionStrategy.FullFile;

                // Mostly format-optimized files
                if (formatOptimizedCount > (count / 3))
                    return CompressionStrategy.FormatOptimized;

                // Large average file -> chunk
                if (avg >= (LARGE_FILE_THRESHOLD_BYTES / 4.0)) // avg > 50MB
                    return CompressionStrategy.Chunk;

                // Default fallback: FullFile for folders with many medium/small files
                return CompressionStrategy.FullFile;
            }
            catch
            {
                return CompressionStrategy.Chunk;
            }
        }

        private static bool IsFormatOptimizedExtension(string ext)
        {
            // known image/audio formats we may specially handle later
            switch (ext)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".webp":
                case ".gif":
                case ".bmp":
                case ".tiff":
                case ".wav":
                case ".flac":
                case ".mp3":
                case ".ogg":
                    return true;
                default:
                    return false;
            }
        }
    }
}
