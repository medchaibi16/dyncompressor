using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace dyncompressor
{
    public static class UltraModeManager
    {
        private const byte FLAG_NONE = 0x00;
        private const byte FLAG_RLE = 0x01;
        private const byte FLAG_DELTA = 0x02;

        private static readonly string[] ExecutableExtensions =
        {
            ".dll", ".exe", ".so", ".dylib",
            ".pdb", ".mdb",
            ".sys", ".ocx", ".drv"
        };

        public class Config
        {
            public int MaxDegreeOfParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount);
            public int ProducerQueueCapacity { get; set; } = 1024;

            public bool EnableRlePreprocessing { get; set; } = false;
            public bool EnableDeltaPreprocessing { get; set; } = false;

            public int MinChunkSize { get; set; } = 64 * 1024;
            public int MaxChunkSize { get; set; } = 2 * 1024 * 1024;
            public long MemoryMapThreshold { get; set; } = 64L * 1024 * 1024;
        }

        private sealed class ChunkJob
        {
            public string FilePath;
            public int ChunkIndex;
            public byte[] Raw;
        }

        private sealed class FileWriteResult
        {
            public string RelativePath;
            public long OriginalSize;
            public List<(int Index, string Method, byte[] Data, byte Flags)> Chunks = new();
        }

        private static bool IsExecutableFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ExecutableExtensions.Contains(ext);
        }

        public static void ProcessEntries(
            IList<string> files,
            string archiveOutput,
            IList<ICompressionAlgorithm> compressors,
            Action<string, long, long, int> progressCallback = null,
            int maxDegreeOfParallelism = 0,
            Config config = null)
        {
            config ??= new Config();
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = config.MaxDegreeOfParallelism;

            string baseDir =
                GetCommonDirectory(files.ToArray())
                ?? Path.GetDirectoryName(files[0])
                ?? Directory.GetCurrentDirectory();

            var chunkQueue = new BlockingCollection<ChunkJob>(config.ProducerQueueCapacity);
            var fileResults = new ConcurrentDictionary<string, FileWriteResult>(StringComparer.OrdinalIgnoreCase);
            var compressedCache = new ConcurrentDictionary<string, (string Method, byte[] Data, byte Flags)>();
            var orderedFiles = files.ToList();

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            // ================= PRODUCER =================
            var producer = Task.Run(() =>
            {
                foreach (var file in orderedFiles)
                {
                    if (!File.Exists(file)) continue;

                    long size = new FileInfo(file).Length;
                    int chunkSize = PickChunkSize(size, config);

                    var fr = new FileWriteResult
                    {
                        RelativePath = Path.GetRelativePath(baseDir, file),
                        OriginalSize = size
                    };
                    fileResults[file] = fr;

                    Stream stream = size >= config.MemoryMapThreshold
                        ? MemoryMappedFile.CreateFromFile(file).CreateViewStream()
                        : new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

                    using (stream)
                    {
                        int index = 0;
                        byte[] buffer = new byte[chunkSize];
                        int read;

                        while ((read = stream.Read(buffer, 0, chunkSize)) > 0)
                        {
                            byte[] raw = read == buffer.Length
                                ? buffer.ToArray()
                                : buffer.Take(read).ToArray();

                            chunkQueue.Add(new ChunkJob
                            {
                                FilePath = file,
                                ChunkIndex = index++,
                                Raw = raw
                            }, token);
                        }
                    }
                }

                chunkQueue.CompleteAdding();
            }, token);

            // ================= WORKERS =================
            var workers = Enumerable.Range(0, maxDegreeOfParallelism)
                .Select(_ => Task.Run(() =>
                {
                    foreach (var job in chunkQueue.GetConsumingEnumerable(token))
                    {
                        byte[] raw = job.Raw;
                        byte flags = FLAG_NONE;

                        string hash = ComputeHash(raw);

                        if (compressedCache.TryGetValue(hash, out var cached))
                        {
                            lock (fileResults[job.FilePath])
                            {
                                fileResults[job.FilePath].Chunks.Add(
                                    (job.ChunkIndex, cached.Method, cached.Data, cached.Flags)
                                );
                            }
                            continue;
                        }

                        if (IsExecutableFile(job.FilePath))
                        {
                            var store = ("STORE", (byte[])raw.Clone(), FLAG_NONE);
                            compressedCache[hash] = store;

                            lock (fileResults[job.FilePath])
                            {
                                fileResults[job.FilePath].Chunks.Add(
                                    (job.ChunkIndex, store.Item1, store.Item2, store.Item3)
                                );
                            }
                            continue;
                        }

                        var profile = ChunkAnalyzer.Analyze(raw);
                        var compressor = ChunkCompressorPicker.SelectBest(profile, job.FilePath)
                                         ?? compressors.First();

                        byte[] compressed = compressor.Compress(raw);

                        if (compressed == null || compressed.Length >= raw.Length)
                        {
                            var store = ("STORE", (byte[])raw.Clone(), FLAG_NONE);
                            compressedCache[hash] = store;

                            lock (fileResults[job.FilePath])
                            {
                                fileResults[job.FilePath].Chunks.Add(
                                    (job.ChunkIndex, store.Item1, store.Item2, store.Item3)
                                );
                            }
                        }
                        else
                        {
                            var entry = (compressor.Name, (byte[])compressed.Clone(), flags);
                            compressedCache[hash] = entry;

                            lock (fileResults[job.FilePath])
                            {
                                fileResults[job.FilePath].Chunks.Add(
                                    (job.ChunkIndex, entry.Item1, entry.Item2, entry.Item3)
                                );
                            }
                        }
                    }
                }, token)).ToArray();

            Task.WaitAll(workers.Prepend(producer).ToArray());

            // ================= WRITER =================
            using var bw = new BinaryWriter(File.Create(archiveOutput));

            bw.Write((byte)3); // format version
            bw.Write(0);       // file count placeholder

            int written = 0;

            foreach (var file in orderedFiles)
            {
                if (!fileResults.TryGetValue(file, out var fr)) continue;

                bw.Write(fr.RelativePath);
                bw.Write(fr.Chunks.Count);

                long compressedSize = 0;

                foreach (var chunk in fr.Chunks.OrderBy(c => c.Index))
                {
                    bw.Write(chunk.Method);
                    bw.Write(chunk.Flags);
                    bw.Write(chunk.Data.Length);
                    bw.Write(chunk.Data);
                    compressedSize += chunk.Data.Length;
                }

                progressCallback?.Invoke(
                    fr.RelativePath,
                    fr.OriginalSize,
                    compressedSize,
                    ++written
                );

                fr.Chunks.Clear();
            }

            // Empty directories
            var emptyDirs = new HashSet<string>();
            foreach (var file in orderedFiles)
            {
                string root = Directory.Exists(file) ? file : Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(root)) continue;

                try
                {
                    foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                            emptyDirs.Add(Path.GetRelativePath(baseDir, dir));
                }
                catch { }
            }

            bw.Write(emptyDirs.Count);
            foreach (var dir in emptyDirs)
                bw.Write(dir);

            bw.Seek(1, SeekOrigin.Begin);
            bw.Write(written);
        }

        private static int PickChunkSize(long size, Config c)
        {
            if (size <= c.MinChunkSize)
                return (int)Math.Max(1, size);

            double t = Math.Log(size + 1, 2);
            double n = Math.Clamp((t - 16) / 10.0, 0, 1);
            return (int)(c.MinChunkSize + n * (c.MaxChunkSize - c.MinChunkSize));
        }

        private static string ComputeHash(byte[] data)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(data));
        }

        private static string GetCommonDirectory(string[] paths)
        {
            if (paths.Length == 0) return null;

            var full = paths.Select(Path.GetFullPath).ToArray();
            string first = full[0];
            int len = first.Length;

            foreach (var p in full.Skip(1))
            {
                int i = 0;
                while (i < len && i < p.Length &&
                       char.ToLowerInvariant(first[i]) == char.ToLowerInvariant(p[i]))
                    i++;
                len = i;
            }

            int sep = first.LastIndexOf(Path.DirectorySeparatorChar, len);
            return sep > 0 ? first[..sep] : Path.GetPathRoot(first);
        }
    }
}
