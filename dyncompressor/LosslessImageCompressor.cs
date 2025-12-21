using System;
using System.Diagnostics;
using System.IO;

namespace dyncompressor
{
    public class LosslessImageCompressor : ICompressionAlgorithm
    {
        public string Name => "LosslessImage";

        public byte[] Compress(byte[] data)
        {
            try
            {
                if (IsPng(data))
                    return OptimizeWithExternalTool(data, "optipng.exe", "png", "-o2 -out {out} {in}");

                if (IsJpeg(data))
                    return OptimizeWithExternalTool(data, "cjpeg.exe", "jpg", "-quality 85 -progressive -outfile {out} {in}");

                if (IsWebP(data))
                    return OptimizeWithExternalTool(data, "cwebp.exe", "webp", "-q 80 -mt {in} -o {out}");

                if (IsGif(data))
                    return OptimizeWithExternalTool(data, "gifsicle.exe", "gif", "--optimize=3 -o {out} {in}");

                // Not a supported image format or tool missing
                return data;
            }
            catch (Exception ex)
            {
                // If optimization fails for ANY reason, return original data
                Console.WriteLine($"Image optimization failed: {ex.Message}");
                return data;
            }
        }

        public byte[] Decompress(byte[] compressedData)
        {
            // External optimizers are lossless, so decompression = just return input
            return compressedData;
        }

        private byte[] OptimizeWithExternalTool(byte[] data, string toolName, string format, string argumentsTemplate)
        {
            string toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
            string toolPath = Path.Combine(toolsDir, toolName);

            // If tool doesn't exist, return original data
            if (!File.Exists(toolPath))
            {
                Console.WriteLine($"Tool not found: {toolPath}");
                return data;
            }

            string tempIn = null;
            string tempOut = null;

            try
            {
                tempIn = Path.ChangeExtension(Path.GetTempFileName(), format);
                tempOut = Path.ChangeExtension(Path.GetTempFileName(), format);

                File.WriteAllBytes(tempIn, data);

                string arguments = argumentsTemplate
                    .Replace("{in}", $"\"{tempIn}\"")
                    .Replace("{out}", $"\"{tempOut}\"");

                var psi = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    // If tool failed, return original data (don't throw exception)
                    if (proc.ExitCode != 0)
                    {
                        Console.WriteLine($"{toolName} failed (exit {proc.ExitCode}): {stderr}");
                        return data;
                    }
                }

                // If output file wasn't created or is empty, return original
                if (!File.Exists(tempOut) || new FileInfo(tempOut).Length == 0)
                {
                    Console.WriteLine($"{toolName} produced no output");
                    return data;
                }

                byte[] optimized = File.ReadAllBytes(tempOut);

                // If optimized is larger, return original
                if (optimized.Length >= data.Length)
                {
                    Console.WriteLine($"{toolName} made file larger, keeping original");
                    return data;
                }

                return optimized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error optimizing with {toolName}: {ex.Message}");
                return data;
            }
            finally
            {
                // Cleanup temp files
                try { if (tempIn != null && File.Exists(tempIn)) File.Delete(tempIn); } catch { }
                try { if (tempOut != null && File.Exists(tempOut)) File.Delete(tempOut); } catch { }
            }
        }

        private bool IsPng(byte[] data) =>
            data.Length > 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;

        private bool IsJpeg(byte[] data) =>
            data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8;

        private bool IsWebP(byte[] data) =>
            data.Length > 12 &&
            data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
            data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P';

        private bool IsGif(byte[] data)
        {
            if (data.Length < 6)
                return false;

            return (data[0] == 'G' && data[1] == 'I' && data[2] == 'F' &&
                    data[3] == '8' &&
                    (data[4] == '7' || data[4] == '9') &&
                    data[5] == 'a');
        }
    }
}