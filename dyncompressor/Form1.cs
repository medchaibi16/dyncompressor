using dyncompressor.Engine;
using Microsoft.WindowsAPICodePack.Dialogs;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dyncompressor
{
    public partial class Form1 : Form
    {
        private volatile bool etaStarted = false;

        public Form1()
        {
            SevenZipBase.SetLibraryPath(@"7z.dll");
            InitializeComponent();

            lvDropBox.View = View.Details;
            lvDropBox.FullRowSelect = true;
            lvDropBox.AllowDrop = true;

            // Designer already adds columns, but ensure they exist
            if (lvDropBox.Columns.Count < 2)
            {
                lvDropBox.Columns.Clear();
                lvDropBox.Columns.Add("Name", 280);
                lvDropBox.Columns.Add("Full Path", 600);
            }

            lvDropBox.DragEnter += LvDropBox_DragEnter;
            lvDropBox.DragDrop += LvDropBox_DragDrop;
        }

        // ----------------- ListView helpers -----------------
        private void AddPathToListView(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            foreach (ListViewItem it in lvDropBox.Items)
            {
                if (it.SubItems.Count > 1 && string.Equals(it.SubItems[1].Text, path, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name)) name = path;

            var item = new ListViewItem(name);
            item.SubItems.Add(path);
            lvDropBox.Items.Add(item);
        }

        private List<string> GetPathsFromListView()
        {
            var list = new List<string>();
            foreach (ListViewItem it in lvDropBox.Items)
            {
                if (it.SubItems.Count > 1)
                    list.Add(it.SubItems[1].Text);
            }
            return list;
        }

        // ----------------- Drag & Drop -----------------
        private void LvDropBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void LvDropBox_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (dropped == null || dropped.Length == 0) return;

                foreach (var p in dropped)
                {
                    if (File.Exists(p) || Directory.Exists(p))
                        AddPathToListView(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error handling dropped items: " + ex.Message);
            }
        }

        // ----------------- Load Path button -----------------
        private void btnLoadPath_Click(object sender, EventArgs e)
        {
            using (var fileDialog = new CommonOpenFileDialog())
            {
                fileDialog.Multiselect = true;
                fileDialog.IsFolderPicker = false;
                fileDialog.Title = "Select files (or Cancel to pick folders instead)";
                if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    foreach (var p in fileDialog.FileNames)
                        AddPathToListView(p);

                    return;
                }
            }

            using (var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                folderDialog.Multiselect = true;
                folderDialog.Title = "Select one or more folders to add";
                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    foreach (var p in folderDialog.FileNames)
                        AddPathToListView(p);
                }
            }
        }

        // ----------------- Smart Full Compress -----------------
        private async void btnSmartFullCompress_Click(object sender, EventArgs e)
        {
            var entries = GetPathsFromListView();
            if (entries == null || entries.Count == 0)
            {
                MessageBox.Show("No files or folders selected. Drag items into the box or use Load Path.");
                return;
            }

            lblTime.Text = "Elapsed: 00:00 | Est: calculating...";
            etaStarted = false;
            stopwatch = Stopwatch.StartNew();

            var uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            uiTimer.Tick += (s, ev) =>
            {
                try
                {
                    this.Invoke((Action)(() =>
                    {
                        if (stopwatch != null && stopwatch.IsRunning)
                        {
                            string current = lblTime?.Text ?? "";
                            int pipeIndex = current.IndexOf('|');
                            string rest = (pipeIndex >= 0) ? " " + current.Substring(pipeIndex).Trim() : " | Est: calculating...";
                            lblTime.Text = $"Elapsed: {stopwatch.Elapsed:mm\\:ss}{rest}";
                        }
                    }));
                }
                catch { }
            };
            uiTimer.Start();

            try
            {
                await Task.Run(() =>
                {
                    List<ICompressionAlgorithm> compressors = new List<ICompressionAlgorithm>
                    {
                        new GzipCompressor(),
                        new BrotliCompressor(),
                        new DeflateCompressor(),
                        new ZipCompressor(),
                        new NoCompression(),
                        new BZip2Compressor(),
                        new LZMACompressor(),
                        new LosslessImageCompressor()
                    };

                    var filesToProcess = new List<string>();
                    foreach (var entry in entries)
                    {
                        if (File.Exists(entry))
                            filesToProcess.Add(entry);
                        else if (Directory.Exists(entry))
                            filesToProcess.AddRange(Directory.GetFiles(entry, "*", SearchOption.AllDirectories));
                    }

                    if (filesToProcess.Count == 0)
                        throw new Exception("No files found to compress.");

                    int totalFiles = filesToProcess.Count;
                    long totalOriginalSize = 0;
                    long totalCompressedSize = 0;

                    string firstEntry = entries[0];
                    string parentDir = File.Exists(firstEntry)
                        ? Path.GetDirectoryName(firstEntry) ?? Directory.GetCurrentDirectory()
                        : Path.GetDirectoryName(firstEntry.TrimEnd(Path.DirectorySeparatorChar)) ?? Directory.GetCurrentDirectory();

                    string archiveBaseName = Path.GetFileName(firstEntry.TrimEnd(Path.DirectorySeparatorChar));
                    if (string.IsNullOrWhiteSpace(archiveBaseName))
                        archiveBaseName = "archive";

                    string archiveOutput = GenerateUniqueFileName(
                        Path.Combine(parentDir, archiveBaseName),
                        archiveBaseName, "com", ".dcz");

                    UltraModeManager.ProcessEntries(
                        filesToProcess,
                        archiveOutput,
                        compressors,
                        (file, originalSize, compressedSize, processedCount) =>
                        {
                            if (originalSize > 0) Interlocked.Add(ref totalOriginalSize, originalSize);
                            if (compressedSize > 0) Interlocked.Add(ref totalCompressedSize, compressedSize);

                            double avgPerFile = stopwatch.Elapsed.TotalSeconds / Math.Max(1, processedCount);
                            double remainingSeconds = avgPerFile * (totalFiles - processedCount);
                            TimeSpan estimatedRemaining = TimeSpan.FromSeconds(remainingSeconds);

                            double ratio = totalOriginalSize > 0
                                ? (100.0 - (totalCompressedSize * 100.0 / (double)totalOriginalSize))
                                : 0;

                            this.Invoke((Action)(() =>
                            {
                                lblTime.Text = processedCount < totalFiles
                                    ? $"Elapsed: {stopwatch.Elapsed:mm\\:ss} | Est: {estimatedRemaining:mm\\:ss} | Files: {processedCount}/{totalFiles} | Savings: {ratio:F2}%"
                                    : $"Elapsed: {stopwatch.Elapsed:mm\\:ss} | Est: done | Files: {processedCount}/{totalFiles} | Savings: {ratio:F2}%";
                            }));
                        },
                        maxDegreeOfParallelism: Environment.ProcessorCount,
                        config: new UltraModeManager.Config
                        {
                            EnableRlePreprocessing = false,
                            EnableDeltaPreprocessing = false,
                            MinChunkSize = 64 * 1024,
                            MaxChunkSize = 2 * 1024 * 1024,
                            MemoryMapThreshold = 64L * 1024 * 1024
                        }
                    );

                    stopwatch.Stop();

                    double finalRatio = totalOriginalSize > 0
                        ? (100.0 - (totalCompressedSize * 100.0 / (double)totalOriginalSize))
                        : 0;

                    this.Invoke((Action)(() =>
                    {
                        lblTime.Text =
                            $"✅ Done! Time: {stopwatch.Elapsed:mm\\:ss} | Original: {FormatSize(totalOriginalSize)} | Compressed: {FormatSize(totalCompressedSize)} | Savings: {finalRatio:F2}% | Archive: {Path.GetFileName(archiveOutput)}";

                        MessageBox.Show($"Compression finished!\nArchive: {archiveOutput}");
                        lvDropBox.Items.Clear();
                    }));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error during compression: " + ex.Message);
            }
            finally
            {
                uiTimer.Stop();
                if (stopwatch != null && stopwatch.IsRunning)
                    stopwatch.Stop();
            }
        }

        // ----------------- Decompress button -----------------
        private void button1_Click(object sender, EventArgs e)
        {
            var entries = GetPathsFromListView()
                .Where(p => string.Equals(Path.GetExtension(p), ".dcz", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (entries.Count == 0)
            {
                MessageBox.Show("No .dcz archives found in the list. Add/select archive(s) first.");
                return;
            }

            foreach (var archive in entries)
            {
                try
                {
                    string outputFolder = Path.Combine(Path.GetDirectoryName(archive), "DecompressedFiles");
                    if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

                    DecompressFiles(archive, outputFolder);
                    MessageBox.Show($"✅ Decompressed {Path.GetFileName(archive)} → {outputFolder}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Failed to decompress {Path.GetFileName(archive)}: {ex.Message}");
                }
            }
        }

        // ----------------- DecompressFiles (WITH PREPROCESSING REVERSAL) -----------------
        public void DecompressFiles(string archivePath, string outputFolder)
        {
            using var reader = new BinaryReader(File.OpenRead(archivePath));

            // Read format version
            byte version = 1;
            long startPos = reader.BaseStream.Position;

            try
            {
                byte firstByte = reader.ReadByte();
                if (firstByte <= 10)
                {
                    version = firstByte;
                    Console.WriteLine($"Archive version: {version}");
                }
                else
                {
                    reader.BaseStream.Seek(startPos, SeekOrigin.Begin);
                }
            }
            catch
            {
                reader.BaseStream.Seek(startPos, SeekOrigin.Begin);
            }

            int fileCount = reader.ReadInt32();
            Console.WriteLine($"Decompressing {fileCount} files...");

            List<ICompressionAlgorithm> allAlgorithms = new List<ICompressionAlgorithm>
    {
        new GzipCompressor(),
        new BrotliCompressor(),
        new DeflateCompressor(),
        new ZipCompressor(),
        new NoCompression(),
        new BZip2Compressor(),
        new LZMACompressor(),
        new LosslessImageCompressor()
    };

            const byte FLAG_RLE = 0x01;
            const byte FLAG_DELTA = 0x02;

            for (int f = 0; f < fileCount; f++)
            {
                string relativePath = reader.ReadString();
                int chunkCount = reader.ReadInt32();

                Console.WriteLine($"[{f + 1}/{fileCount}] {relativePath} ({chunkCount} chunks)");

                var reconstructed = new List<byte>();

                for (int c = 0; c < chunkCount; c++)
                {
                    string methodName = reader.ReadString();

                    // Read preprocessing flags (Version 3+)
                    byte flags = 0;
                    if (version >= 3)
                    {
                        flags = reader.ReadByte();
                    }

                    int size = reader.ReadInt32();
                    byte[] compressedData = reader.ReadBytes(size);
                    // ✅ CRITICAL: Verify we read the expected amount
                    if (compressedData.Length != size)
                    {
                        throw new Exception($"Failed to read chunk {c}: expected {size} bytes, got {compressedData.Length}");
                    }

                    Console.WriteLine($"  Chunk {c}: Method={methodName}, Flags=0x{flags:X2}, Size={size}");

                    byte[] decompressed;

                    // Step 1: Decompress
                    if (methodName == "STORE")
                    {
                        decompressed = compressedData;
                        Console.WriteLine($"  STORE chunk (no decompression): {decompressed.Length} bytes");
                    }
                    else
                    {
                        var method = allAlgorithms.FirstOrDefault(m => m.Name == methodName);
                        if (method == null)
                            throw new Exception($"Missing decompressor: {methodName}");

                        try
                        {
                            decompressed = method.Decompress(compressedData);

                            // ✅ CRITICAL: Check if decompression returned null or invalid data
                            if (decompressed == null)
                            {
                                throw new Exception($"Decompressor {methodName} returned null");
                            }

                            Console.WriteLine($"  Decompressed with {methodName}: {decompressed.Length} bytes");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ❌ DECOMPRESSION FAILED: {ex.Message}");
                            throw new Exception($"Failed to decompress {relativePath} chunk {c} with {methodName}: {ex.Message}", ex);
                        }
                    }

                    Console.WriteLine($"    After decompress: {decompressed.Length} bytes");

                    // Step 2: Reverse preprocessing in CORRECT order
                    // Compression order was: Raw → RLE → Delta → Compress
                    // So decompression must be: Decompress → Reverse Delta → Reverse RLE

                    if ((flags & FLAG_DELTA) != 0)
                    {
                        Console.WriteLine($"    Reversing Delta encoding...");
                        decompressed = DeltaPreprocessor.Decode(decompressed);
                        Console.WriteLine($"    After Delta decode: {decompressed.Length} bytes");
                    }

                    if ((flags & FLAG_RLE) != 0)
                    {
                        Console.WriteLine($"    Reversing RLE encoding...");
                        decompressed = RlePreprocessor.Decode(decompressed);
                        Console.WriteLine($"    After RLE decode: {decompressed.Length} bytes");
                    }

                    reconstructed.AddRange(decompressed);
                }

                string outPath = Path.Combine(outputFolder, relativePath);
                string outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                File.WriteAllBytes(outPath, reconstructed.ToArray());
                Console.WriteLine($"  ✅ Written: {outPath} ({reconstructed.Count} bytes)");
            }

            // Restore empty directories (Version 2+)
            if (version >= 2)
            {
                try
                {
                    int emptyDirCount = reader.ReadInt32();
                    Console.WriteLine($"Restoring {emptyDirCount} empty directories...");

                    for (int i = 0; i < emptyDirCount; i++)
                    {
                        string dirPath = reader.ReadString();
                        string fullPath = Path.Combine(outputFolder, dirPath);
                        if (!Directory.Exists(fullPath))
                        {
                            Directory.CreateDirectory(fullPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not restore empty directories: {ex.Message}");
                }
            }

            Console.WriteLine("✅ Decompression complete!");
            lvDropBox.Items.Clear();
        }

        // ----------------- Utilities -----------------
        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GenerateUniqueFileName(string basePath, string baseName, string suffix, string extension)
        {
            string dir = Path.GetDirectoryName(basePath) ?? Directory.GetCurrentDirectory();
            string nameWithoutExt = Path.GetFileNameWithoutExtension(baseName);
            string candidateName = $"{nameWithoutExt}_{suffix}{extension}";
            string fullPath = Path.Combine(dir, candidateName);
            int counter = 1;
            while (File.Exists(fullPath))
            {
                candidateName = $"{nameWithoutExt}_{suffix}({counter}){extension}";
                fullPath = Path.Combine(dir, candidateName);
                counter++;
            }
            return fullPath;
        }

        // ----------------- Clear Button -----------------
        private void button2_Click(object sender, EventArgs e)
        {
            lvDropBox.Items.Clear();
        }
    }
}