using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace dyncompressor
{
    public static class FileClassifier
    {
        // quick list of extensions that are usually already compressed or archives
        private static readonly HashSet<string> CommonCompressedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip",".rar",".7z",".gz",".tgz",".bz2",".xz",
            ".mp3",".ogg",".mp4",".mkv",".webm",
            ".jpg",".jpeg",".png",".gif",".bmp",".ico",".tga",".dds",".pkm",".astc",".basis",
            ".wav", ".flac",
            ".pak", ".pak0", ".pak1", ".bundle", ".unity3d", ".assets", ".dat" // game packages (conservative)
        };

        // signature checks for some formats (magic bytes)
        public static bool LooksLikeArchiveOrCompressedStream(byte[] header)
        {
            if (header == null || header.Length < 4) return false;
            // PK.. => zip
            if (header[0] == 0x50 && header[1] == 0x4B) return true;
            // RAR
            if (header.Length >= 7 && header.Take(7).SequenceEqual(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 })) return true;
            // 7z
            if (header.Length >= 6 && header.Take(6).SequenceEqual(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C })) return true;
            // PNG
            if (header.Length >= 8 && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return true;
            // JPG
            if (header[0] == 0xFF && header[1] == 0xD8) return true;
            // gzip
            if (header[0] == 0x1F && header[1] == 0x8B) return true;
            // zlib (often)
            if (header.Length >= 2 && header[0] == 0x78) return true;
            return false;
        }

        // compute Shannon entropy for a byte array (0..8)
        public static double ShannonEntropy(byte[] data)
        {
            if (data == null || data.Length == 0) return 0.0;
            var counts = new long[256];
            foreach (var b in data) counts[b]++;
            double len = data.Length;
            double entropy = 0.0;
            for (int i = 0; i < 256; i++)
            {
                if (counts[i] == 0) continue;
                double p = counts[i] / len;
                entropy -= p * Math.Log(p, 2);
            }
            return entropy; // bits per byte (0..8)
        }

        // sample some parts of file (start, middle, end) up to 'sampleSize' per sample
        public static byte[] SampleFile(string path, int sampleSize = 65536)
        {
            try
            {
                using var fs = File.OpenRead(path);
                long len = fs.Length;
                if (len <= sampleSize) // small file: return entire file
                {
                    using var ms = new MemoryStream();
                    fs.CopyTo(ms);
                    return ms.ToArray();
                }

                var piece = new List<byte>();
                // start
                fs.Seek(0, SeekOrigin.Begin);
                var buf = new byte[sampleSize];
                int read = fs.Read(buf, 0, sampleSize);
                piece.AddRange(buf.Take(read));

                // middle
                long mid = Math.Max(0, len / 2 - sampleSize / 2);
                fs.Seek(mid, SeekOrigin.Begin);
                read = fs.Read(buf, 0, sampleSize);
                piece.AddRange(buf.Take(read));

                // end
                long tail = Math.Max(0, len - sampleSize);
                fs.Seek(tail, SeekOrigin.Begin);
                read = fs.Read(buf, 0, sampleSize);
                piece.AddRange(buf.Take(read));

                return piece.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        // top-level "is probably already compressed" quick check
        public static bool IsProbablyAlreadyCompressed(string path)
        {
            try
            {
                var ext = Path.GetExtension(path);
                if (!string.IsNullOrEmpty(ext) && CommonCompressedExtensions.Contains(ext)) return true;

                var header = new byte[32];
                using var fs = File.OpenRead(path);
                int got = fs.Read(header, 0, header.Length);
                if (got > 0)
                {
                    return LooksLikeArchiveOrCompressedStream(header);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
