// ChunkCompressor.cs
using dyncompressor.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dyncompressor
{
    public class ChunkCompressor
    {
        private readonly DynamicCompressor _dynamicCompressor;

        private const long FULLFILE_MAX_BYTES = 500 * 1024 * 1024;
        private const long FULLFILE_SAMPLE_LIMIT = 256 * 1024;

        public ChunkCompressor(DynamicCompressor dynamicCompressor)
        {
            _dynamicCompressor = dynamicCompressor;
        }

        public List<CompressedChunk> CompressFile(string inputFilePath)
        {
            var chunks = FileChunker.SplitFile(inputFilePath);
            List<CompressedChunk> compressedChunks = new List<CompressedChunk>();

            foreach (var chunk in chunks)
            {
                var profile = ChunkAnalyzer.Analyze(chunk);
                var selectedCompressor = ChunkCompressorPicker.SelectBest(profile, inputFilePath);
                var compressed = selectedCompressor.Compress(chunk);

                compressedChunks.Add(new CompressedChunk
                {
                    MethodName = selectedCompressor.Name,
                    CompressedData = compressed
                });
            }

            return compressedChunks;
        }

        public void CompressFiles(string[] files, string outputFilePath, Action<string, long, long, int> progressCallback = null)
        {
            if (files == null || files.Length == 0)
                throw new ArgumentException("No files provided.", nameof(files));

            List<ICompressionAlgorithm> candidateList = new List<ICompressionAlgorithm>
            {
                new LZMACompressor(),
                new BrotliCompressor(),
                new GzipCompressor(),
                new DeflateCompressor(),
                new BZip2Compressor(),
                new LosslessImageCompressor(),
                new NoCompression()
            };
            ICompressionAlgorithm[] candidates = candidateList.ToArray();

            string baseDir = GetCommonDirectory(files) ?? Path.GetDirectoryName(files[0]) ?? Directory.GetCurrentDirectory();

            string tempFile = outputFilePath + ".tmp";
            var fileEntries = new List<(string RelativePath, long OriginalSize, long CompressedSize)>();

            using (var output = new BinaryWriter(File.Create(tempFile)))
            {
                output.Write(0); // placeholder

                int processedFiles = 0;

                foreach (var file in files)
                {
                    if (Directory.Exists(file))
                        continue;

                    string relativePath;
                    try { relativePath = Path.GetRelativePath(baseDir, file); }
                    catch { relativePath = Path.GetFileName(file); }

                    long originalSize = new FileInfo(file).Length;
                    long compressedTotalForFile = 0;

                    var decision = DecisionEngine.Decide(file, candidates, out ICompressionAlgorithm chosenAlgo);

                    if (decision == CompressionDecision.Store)
                    {
                        byte[] data = File.ReadAllBytes(file);
                        output.Write(relativePath);
                        output.Write(1);
                        output.Write("STORE");
                        output.Write(data.Length);
                        output.Write(data);
                        compressedTotalForFile = data.LongLength;
                    }
                    else if (decision == CompressionDecision.ImageSpecial)
                    {
                        ICompressionAlgorithm imageComp = candidateList.FirstOrDefault(c => c.Name == "LosslessImage");
                        if (imageComp == null) imageComp = chosenAlgo;
                        byte[] raw = File.ReadAllBytes(file);
                        byte[] outBytes = imageComp != null ? imageComp.Compress(raw) : raw;
                        output.Write(relativePath);
                        output.Write(1);
                        string methodName = imageComp != null ? imageComp.Name : "STORE";
                        output.Write(methodName);
                        output.Write(outBytes.Length);
                        output.Write(outBytes);
                        compressedTotalForFile = outBytes.LongLength;
                    }
                    else if (decision == CompressionDecision.FullCompress)
                    {
                        ICompressionAlgorithm algo = chosenAlgo ?? PreCompressionTester.ChooseBestCompressorBySample(FileClassifier.SampleFile(file, 64 * 1024), candidates, 0.995);
                        if (algo == null) algo = candidateList[0];

                        byte[] raw = File.ReadAllBytes(file);
                        byte[] compressed = algo.Compress(raw);

                        if (compressed == null || compressed.Length >= raw.Length)
                        {
                            output.Write(relativePath);
                            output.Write(1);
                            output.Write("STORE");
                            output.Write(raw.Length);
                            output.Write(raw);
                            compressedTotalForFile = raw.LongLength;
                        }
                        else
                        {
                            output.Write(relativePath);
                            output.Write(1);
                            output.Write(algo.Name);
                            output.Write(compressed.Length);
                            output.Write(compressed);
                            compressedTotalForFile = compressed.LongLength;
                        }
                    }
                    else
                    {
                        var chunks = FileChunker.SplitFile(file);
                        output.Write(relativePath);
                        output.Write(chunks.Count);

                        foreach (var chunk in chunks)
                        {
                            var profile = ChunkAnalyzer.Analyze(chunk);
                            var selectedCompressor = ChunkCompressorPicker.SelectBest(profile, file);  // ✅ FIXED!

                            if (selectedCompressor == null)
                            {
                                output.Write("STORE");
                                output.Write(chunk.Length);
                                output.Write(chunk);
                                compressedTotalForFile += chunk.LongLength;
                            }
                            else
                            {
                                byte[] compressedChunk = selectedCompressor.Compress(chunk);
                                if (compressedChunk == null || compressedChunk.Length >= chunk.Length)
                                {
                                    output.Write("STORE");
                                    output.Write(chunk.Length);
                                    output.Write(chunk);
                                    compressedTotalForFile += chunk.LongLength;
                                }
                                else
                                {
                                    output.Write(selectedCompressor.Name);
                                    output.Write(compressedChunk.Length);
                                    output.Write(compressedChunk);
                                    compressedTotalForFile += compressedChunk.LongLength;
                                }
                            }
                        }
                    }

                    processedFiles++;
                    fileEntries.Add((relativePath, originalSize, compressedTotalForFile));

                    progressCallback?.Invoke(relativePath, originalSize, compressedTotalForFile, processedFiles);
                    output.Flush();
                }

                output.Seek(0, SeekOrigin.Begin);
                output.Write(fileEntries.Count);
            }

            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);
            File.Move(tempFile, outputFilePath);
        }

        private string GetCommonDirectory(string[] paths)
        {
            if (paths == null || paths.Length == 0) return null;

            var full = paths.Select(p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar)).ToArray();

            string first = full[0];
            int idx = first.Length;

            for (int i = 1; i < full.Length; i++)
            {
                int j = 0;
                int limit = Math.Min(first.Length, full[i].Length);
                while (j < limit && char.ToLowerInvariant(first[j]) == char.ToLowerInvariant(full[i][j]))
                    j++;
                idx = Math.Min(idx, j);
            }

            if (idx == 0)
                return Path.GetPathRoot(first);

            int lastSep = first.LastIndexOf(Path.DirectorySeparatorChar, idx - 1);
            if (lastSep < 0)
                return Path.GetPathRoot(first);

            return first.Substring(0, lastSep + 1).TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}