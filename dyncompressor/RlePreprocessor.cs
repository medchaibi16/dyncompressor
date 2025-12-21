using System;
using System.Collections.Generic;

namespace dyncompressor
{
    public static class RlePreprocessor
    {
        private const byte ESCAPE_BYTE = 0xFF;
        private const int MIN_RUN_LENGTH = 4; // Only encode runs of 4+ bytes

        public static byte[] Encode(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            var output = new List<byte>(data.Length);
            int i = 0;

            while (i < data.Length)
            {
                byte current = data[i];
                int runLength = 1;

                // Count consecutive identical bytes
                while (i + runLength < data.Length &&
                       data[i + runLength] == current &&
                       runLength < 255)
                {
                    runLength++;
                }

                if (runLength >= MIN_RUN_LENGTH)
                {
                    // Encode as: ESCAPE_BYTE, value, count
                    output.Add(ESCAPE_BYTE);
                    output.Add(current);
                    output.Add((byte)runLength);
                    i += runLength;
                }
                else
                {
                    // Output literal bytes
                    for (int j = 0; j < runLength; j++)
                    {
                        output.Add(current);

                        // If the literal is ESCAPE_BYTE, escape it
                        if (current == ESCAPE_BYTE)
                        {
                            output.Add(0); // Marker for literal escape byte
                        }
                    }
                    i += runLength;
                }
            }

            return output.ToArray();
        }

        public static byte[] Decode(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            var output = new List<byte>(data.Length * 2);
            int i = 0;

            while (i < data.Length)
            {
                if (data[i] == ESCAPE_BYTE && i + 1 < data.Length)
                {
                    if (i + 2 < data.Length && data[i + 2] > 0)
                    {
                        // RLE sequence: ESCAPE_BYTE, value, count
                        byte value = data[i + 1];
                        int count = data[i + 2];

                        for (int j = 0; j < count; j++)
                            output.Add(value);

                        i += 3;
                    }
                    else if (i + 1 < data.Length && data[i + 1] == 0)
                    {
                        // Literal ESCAPE_BYTE
                        output.Add(ESCAPE_BYTE);
                        i += 2;
                    }
                    else
                    {
                        // Malformed, output as-is
                        output.Add(data[i]);
                        i++;
                    }
                }
                else
                {
                    output.Add(data[i]);
                    i++;
                }
            }

            return output.ToArray();
        }

        // Check if data would benefit from RLE
        public static bool WouldBenefit(byte[] data)
        {
            if (data == null || data.Length < 100) return false;

            int runs = 0;
            int i = 0;

            while (i < data.Length)
            {
                byte current = data[i];
                int runLength = 1;

                while (i + runLength < data.Length &&
                       data[i + runLength] == current &&
                       runLength < 255)
                {
                    runLength++;
                }

                if (runLength >= MIN_RUN_LENGTH)
                    runs++;

                i += runLength;
            }

            // If more than 5% of data is runs, it would benefit
            return runs > (data.Length / 100 * 5);
        }
    }
}