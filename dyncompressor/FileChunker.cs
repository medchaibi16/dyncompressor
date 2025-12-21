using System;
using System.Collections.Generic;
using System.IO;

namespace dyncompressor
{
    public class FileChunker
    {
        public static int GetOptimalChunkSize(long fileSize)
        {
            const int MB = 1024 * 1024;
            if (fileSize < 10 * MB)
                return 512 * 1024; // 512KB
            else if (fileSize < 100 * MB)
                return 2 * MB;     // 2MB
            else
                return 8 * MB;     // 8MB
        }

        public static List<byte[]> SplitFile(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // ✅ If it's an image, bypass splitting
            if (ChunkAnalyzer.IsImageData(fileBytes))
            {
                return new List<byte[]> { fileBytes };
            }

            // Otherwise split by optimal chunk size
            var chunks = new List<byte[]>();
            long fileSize = fileBytes.Length;
            int chunkSize = GetOptimalChunkSize(fileSize);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);
                    chunks.Add(chunk);
                }
            }

            return chunks;
        }
    }
}
