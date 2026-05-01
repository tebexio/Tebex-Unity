using System;
using System.Collections.Generic;
using System.Text;

namespace Tebex.QR
{
    public sealed class QrCode
    {
        // raw bytes of the encoded structure:
        // [0]=version, [1]=ecLevel(0=L), [2]=size, [3..]=packed module bits row-major (1=black)
        public byte[] Bytes;

        private QrCode(byte[] bytes) => Bytes = bytes;

        // Public helper: reconstruct module matrix from Bytes
        public bool[,] GetModules()
        {
            int version = Bytes[0];
            int size = Bytes[2];
            var modules = new bool[size, size];
            int bitIndex = 0;
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                int byteIndex = 3 + (bitIndex >> 3);
                int bitInByte = 7 - (bitIndex & 7);
                bool black = ((Bytes[byteIndex] >> bitInByte) & 1) != 0;
                modules[r, c] = black;
                bitIndex++;
            }
            return modules;
        }

        /// <summary>
        /// Encode content (URL) into a basic QR (Byte mode), versions 1..4, EC Level L.
        /// </summary>
        public static QrCode Encode(string content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            if (!content.StartsWith("https://"))
            {
                throw new ArgumentException("Content must be a web address beginning with https://, got " + content);
            }

            if (content.Contains("javascript:"))
            {
                throw new ArgumentException("Content must not contain Javascript");
            }

            if (content.Contains("<script>"))
            {
                throw new ArgumentException("Content must not contain script tags");
            }

            if (content.Contains("eval(") || content.Contains("onerror=") || content.Contains("onload=") ||
                content.Contains("document."))
            {
                throw new ArgumentException("Invalid content url");
            }
            
            // Byte mode payload (UTF-8)
            byte[] dataBytes = Encoding.UTF8.GetBytes(content);

            // Choose minimal version (1..4) that fits in Byte mode with EC level L.
            // Capacities in bytes for Byte mode, Level L (versions 1..4):
            // v1: 17, v2: 32, v3: 53, v4: 78
            int version = ChooseVersionByteModeL(dataBytes.Length);
            if (version == 0)
                throw new ArgumentException("Content too long for versions 1..4 at EC Level L in this basic implementation.");

            int size = 21 + 4 * (version - 1);

            // Get codeword parameters for version + EC L.
            // For versions 1..4, Level L, single block:
            // v1: data=19, ecc=7, total=26
            // v2: data=34, ecc=10, total=44
            // v3: data=55, ecc=15, total=70
            // v4: data=80, ecc=20, total=100
            GetV1to4LParams(version, out int dataCodewords, out int eccCodewords, out int totalCodewords);

            // 1) Build data bitstream (Mode + Count + Data + Terminator + Padding)
            var bb = new BitBuffer();
            bb.AppendBits(0b0100, 4); // Byte mode
            bb.AppendBits(dataBytes.Length, 8); // versions 1..9 => 8-bit count in byte mode
            foreach (byte b in dataBytes) bb.AppendBits(b, 8);

            int totalDataBits = dataCodewords * 8;

            // Terminator up to 4 bits
            int remaining = totalDataBits - bb.BitLength;
            if (remaining > 0)
                bb.AppendBits(0, Math.Min(4, remaining));

            // Pad to byte boundary
            while ((bb.BitLength & 7) != 0) bb.AppendBits(0, 1);

            // Pad bytes 0xEC, 0x11 alternation until full
            bool toggle = true;
            while (bb.BitLength < totalDataBits)
            {
                bb.AppendBits(toggle ? 0xEC : 0x11, 8);
                toggle = !toggle;
            }

            byte[] dataCw = bb.ToBytes(); // exactly dataCodewords length
            if (dataCw.Length != dataCodewords)
                throw new InvalidOperationException("Unexpected data codeword length.");

            // 2) Reed-Solomon ECC (GF(256), poly 0x11D)
            byte[] eccCw = ReedSolomon.ComputeECC(dataCw, eccCodewords);

            // 3) Interleave (single block here => just concat)
            var allCw = new byte[totalCodewords];
            Buffer.BlockCopy(dataCw, 0, allCw, 0, dataCw.Length);
            Buffer.BlockCopy(eccCw, 0, allCw, dataCw.Length, eccCw.Length);

            // 4) Build base matrix with function patterns
            var modules = new bool[size, size];
            var isFunction = new bool[size, size];

            DrawFunctionPatterns(version, modules, isFunction);

            // 5) Place codewords into matrix (unmasked initially)
            PlaceCodewords(modules, isFunction, allCw);

            // 6) Choose best mask (0..7) using penalty scoring
            int bestMask = -1;
            int bestPenalty = int.MaxValue;
            bool[,] best = null;

            for (int mask = 0; mask < 8; mask++)
            {
                var temp = (bool[,])modules.Clone();
                ApplyMask(temp, isFunction, mask);
                // Write format bits for EC=L and chosen mask
                WriteFormatInfo(temp, isFunction, ecLevelBits: 0b01, mask);
                int penalty = PenaltyScore(temp);
                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestMask = mask;
                    best = temp;
                }
            }

            // 7) Pack into Bytes
            byte[] packed = PackModules(best);
            // bytes header: version, ecLevel=0(L), size
            byte[] output = new byte[3 + packed.Length];
            output[0] = (byte)version;
            output[1] = 0; // L
            output[2] = (byte)size;
            Buffer.BlockCopy(packed, 0, output, 3, packed.Length);

            return new QrCode(output);
        }

        /// <summary>
        /// Decode from this QrCode’s module bytes.
        /// This is a “clean decode” for codes produced by this encoder:
        /// - reads format to get mask
        /// - unmask
        /// - reads codewords
        /// - ignores ECC correction (assumes no errors)
        /// - parses Byte mode
        /// </summary>
        public static string Decode(QrCode code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));

            int version = code.Bytes[0];
            int size = code.Bytes[2];
            var modules = code.GetModules();

            // Rebuild function map the same way
            var isFunction = new bool[size, size];
            var dummy = new bool[size, size];
            DrawFunctionPatterns(version, dummy, isFunction);

            // Read format info to get mask (we assume EC=L)
            int mask = ReadMaskFromFormat(modules);

            // Unmask data modules
            ApplyMask(modules, isFunction, mask);

            // Extract codewords
            GetV1to4LParams(version, out int dataCodewords, out int eccCodewords, out int totalCodewords);
            byte[] allCw = ReadCodewords(modules, isFunction, totalCodewords);

            // Take data codewords only (ignore ECC in this basic decoder)
            byte[] dataCw = new byte[dataCodewords];
            Buffer.BlockCopy(allCw, 0, dataCw, 0, dataCodewords);

            // Parse bitstream
            var br = new BitReader(dataCw);

            int mode = br.ReadBits(4);
            if (mode == 0) return ""; // terminator
            if (mode != 0b0100) throw new NotSupportedException("Only Byte mode is supported by this basic decoder.");

            int count = br.ReadBits(8);
            var payload = new byte[count];
            for (int i = 0; i < count; i++)
                payload[i] = (byte)br.ReadBits(8);

            return Encoding.UTF8.GetString(payload);
        }

        // -------------------- Version selection & parameters --------------------

        private static int ChooseVersionByteModeL(int byteLen)
        {
            if (byteLen <= 17) return 1;
            if (byteLen <= 32) return 2;
            if (byteLen <= 53) return 3;
            if (byteLen <= 78) return 4;
            return 0;
        }

        private static void GetV1to4LParams(int version, out int data, out int ecc, out int total)
        {
            switch (version)
            {
                case 1: data = 19; ecc = 7; total = 26; return;
                case 2: data = 34; ecc = 10; total = 44; return;
                case 3: data = 55; ecc = 15; total = 70; return;
                case 4: data = 80; ecc = 20; total = 100; return;
                default: throw new NotSupportedException("This basic implementation supports versions 1..4 only.");
            }
        }

        // -------------------- Function patterns --------------------

        private static void DrawFunctionPatterns(int version, bool[,] modules, bool[,] isFunction)
        {
            int size = modules.GetLength(0);

            // Finder patterns + separators
            DrawFinder(modules, isFunction, 0, 0);
            DrawFinder(modules, isFunction, 0, size - 7);
            DrawFinder(modules, isFunction, size - 7, 0);

            DrawSeparators(isFunction, 0, 0);
            DrawSeparators(isFunction, 0, size - 7);
            DrawSeparators(isFunction, size - 7, 0);

            // Timing patterns
            for (int i = 8; i < size - 8; i++)
            {
                bool val = (i % 2) == 0;
                modules[6, i] = val; isFunction[6, i] = true;
                modules[i, 6] = val; isFunction[i, 6] = true;
            }

            // Dark module (row = 4*version + 9, col = 8) in 0-based coords
            int darkRow = 4 * version + 9;
            modules[darkRow, 8] = true;
            isFunction[darkRow, 8] = true;

            // Reserve format info areas (we’ll write actual bits later)
            ReserveFormatAreas(isFunction);

            // (No alignment patterns for versions 1..4 except v2+ has one,
            // but to keep “basic” and still valid, we SHOULD draw them.)
            DrawAlignmentPatterns(version, modules, isFunction);

            // Version info (only version >= 7, so not needed here)
        }

        private static void DrawFinder(bool[,] modules, bool[,] isFunction, int top, int left)
        {
            // 7x7 finder
            for (int r = 0; r < 7; r++)
            for (int c = 0; c < 7; c++)
            {
                int rr = top + r, cc = left + c;
                bool on =
                    (r == 0 || r == 6 || c == 0 || c == 6) ||
                    (r >= 2 && r <= 4 && c >= 2 && c <= 4);

                modules[rr, cc] = on;
                isFunction[rr, cc] = true;
            }
        }

        private static void DrawSeparators(bool[,] isFunction, int top, int left)
        {
            int size = isFunction.GetLength(0);

            // One-module white border around finder (mark as function so data won't go there)
            for (int i = -1; i <= 7; i++)
            {
                SetFunc(isFunction, top - 1, left + i, size);
                SetFunc(isFunction, top + 7, left + i, size);
                SetFunc(isFunction, top + i, left - 1, size);
                SetFunc(isFunction, top + i, left + 7, size);
            }
        }

        private static void SetFunc(bool[,] isFunction, int r, int c, int size)
        {
            if ((uint)r < (uint)size && (uint)c < (uint)size)
                isFunction[r, c] = true;
        }

        private static void ReserveFormatAreas(bool[,] isFunction)
        {
            int size = isFunction.GetLength(0);

            // Format bits around top-left
            for (int i = 0; i < 9; i++)
            {
                if (i != 6)
                {
                    isFunction[8, i] = true;
                    isFunction[i, 8] = true;
                }
            }

            // Format bits near top-right and bottom-left
            for (int i = 0; i < 8; i++)
            {
                isFunction[8, size - 1 - i] = true;
                isFunction[size - 1 - i, 8] = true;
            }

            // The module at (8,8) is format too
            isFunction[8, 8] = true;
        }

        private static void DrawAlignmentPatterns(int version, bool[,] modules, bool[,] isFunction)
        {
            if (version == 1) return;

            // For versions 2..4, there is exactly one alignment pattern centered at:
            // v2: (18,18), v3: (22,22), v4: (26,26) in 0-based within size 25/29/33
            // More generally: the single alignment center is size-7.
            int size = modules.GetLength(0);
            int center = size - 7;

            // Don’t draw if it overlaps a finder area (it won’t for v2..4)
            DrawAlignment(modules, isFunction, center - 2, center - 2);
        }

        private static void DrawAlignment(bool[,] modules, bool[,] isFunction, int top, int left)
        {
            // 5x5 alignment: black border, white inside border, black center
            for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
            {
                int rr = top + r, cc = left + c;
                bool on =
                    (r == 0 || r == 4 || c == 0 || c == 4) ||
                    (r == 2 && c == 2);

                modules[rr, cc] = on;
                isFunction[rr, cc] = true;
            }
        }

        // -------------------- Data placement --------------------

        private static void PlaceCodewords(bool[,] modules, bool[,] isFunction, byte[] codewords)
        {
            int size = modules.GetLength(0);

            int bitIndex = 0;
            int dir = -1; // moving up initially
            int col = size - 1;

            while (col > 0)
            {
                if (col == 6) col--; // skip timing column

                for (int row = (dir == -1 ? size - 1 : 0);
                     (dir == -1 ? row >= 0 : row < size);
                     row += dir)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        int x = col - c;
                        if (isFunction[row, x]) continue;

                        bool bit = GetBit(codewords, bitIndex++);
                        modules[row, x] = bit;
                    }
                }

                col -= 2;
                dir = -dir;
            }
        }

        private static bool GetBit(byte[] data, int bitIndex)
        {
            int byteIndex = bitIndex >> 3;
            int bitInByte = 7 - (bitIndex & 7);
            if (byteIndex >= data.Length) return false;
            return ((data[byteIndex] >> bitInByte) & 1) != 0;
        }

        private static byte[] ReadCodewords(bool[,] modules, bool[,] isFunction, int totalCodewords)
        {
            int size = modules.GetLength(0);
            int totalBits = totalCodewords * 8;
            var outBytes = new byte[totalCodewords];

            int bitIndex = 0;
            int dir = -1;
            int col = size - 1;

            while (col > 0 && bitIndex < totalBits)
            {
                if (col == 6) col--;

                for (int row = (dir == -1 ? size - 1 : 0);
                     (dir == -1 ? row >= 0 : row < size);
                     row += dir)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        int x = col - c;
                        if (isFunction[row, x]) continue;
                        if (bitIndex >= totalBits) break;

                        bool bit = modules[row, x];
                        int byteIndex = bitIndex >> 3;
                        int bitInByte = 7 - (bitIndex & 7);
                        if (bit) outBytes[byteIndex] |= (byte)(1 << bitInByte);
                        bitIndex++;
                    }
                    if (bitIndex >= totalBits) break;
                }

                col -= 2;
                dir = -dir;
            }

            return outBytes;
        }

        // -------------------- Masking & format info --------------------

        private static void ApplyMask(bool[,] modules, bool[,] isFunction, int mask)
        {
            int size = modules.GetLength(0);
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (isFunction[r, c]) continue;
                if (MaskBit(mask, r, c))
                    modules[r, c] = !modules[r, c];
            }
        }

        private static bool MaskBit(int mask, int r, int c)
        {
            // Standard 8 masks
            return mask switch
            {
                0 => ((r + c) & 1) == 0,
                1 => (r & 1) == 0,
                2 => (c % 3) == 0,
                3 => ((r + c) % 3) == 0,
                4 => (((r / 2) + (c / 3)) & 1) == 0,
                5 => ((r * c) % 2 + (r * c) % 3) == 0,
                6 => ((((r * c) % 2) + ((r * c) % 3)) & 1) == 0,
                7 => ((((r + c) % 2) + ((r * c) % 3)) & 1) == 0,
                _ => throw new ArgumentOutOfRangeException(nameof(mask))
            };
        }

        private static void WriteFormatInfo(bool[,] modules, bool[,] isFunction, int ecLevelBits, int mask)
        {
            // Format bits: 15 bits = BCH(5)->(15) of (EC(2)+mask(3)), XOR with 0x5412.
            int format = (ecLevelBits << 3) | mask; // 5 bits
            int bch = ComputeBCH(format, 0b10100110111, 10); // generator poly for format info
            int bits15 = ((format << 10) | bch) ^ 0x5412;

            int size = modules.GetLength(0);

            // Place into the two standard locations.
            // Top-left around timing:
            // (8,0..5), (8,7), (8,8), (7,8), (5..0,8)
            int[] rPos = { 8,8,8,8,8,8,8,8,7,5,4,3,2,1,0 };
            int[] cPos = { 0,1,2,3,4,5,7,8,8,8,8,8,8,8,8 };

            for (int i = 0; i < 15; i++)
            {
                bool bit = ((bits15 >> (14 - i)) & 1) != 0;
                modules[rPos[i], cPos[i]] = bit;
                isFunction[rPos[i], cPos[i]] = true;
            }

            // Top-right / bottom-left copy:
            // (8, size-1..size-8) and (size-1..size-7, 8)
            for (int i = 0; i < 8; i++)
            {
                bool bit = ((bits15 >> i) & 1) != 0;
                modules[8, size - 1 - i] = bit;
                isFunction[8, size - 1 - i] = true;
            }
            for (int i = 8; i < 15; i++)
            {
                bool bit = ((bits15 >> i) & 1) != 0;
                modules[size - 15 + i, 8] = bit; // maps i=8..14 to rows size-7..size-1
                isFunction[size - 15 + i, 8] = true;
            }
        }

        private static int ReadMaskFromFormat(bool[,] modules)
        {
            // Read the 15 format bits from the top-left pattern positions we wrote.
            int[] rPos = { 8,8,8,8,8,8,8,8,7,5,4,3,2,1,0 };
            int[] cPos = { 0,1,2,3,4,5,7,8,8,8,8,8,8,8,8 };

            int bits15 = 0;
            for (int i = 0; i < 15; i++)
            {
                bits15 <<= 1;
                if (modules[rPos[i], cPos[i]]) bits15 |= 1;
            }
            bits15 ^= 0x5412;
            int format = bits15 >> 10;
            return format & 0b111;
        }

        private static int ComputeBCH(int value, int poly, int polyShift)
        {
            // Compute remainder of (value << polyShift) divided by poly
            int v = value << polyShift;
            int msbPoly = HighestBit(poly);
            while (HighestBit(v) >= msbPoly)
            {
                int shift = HighestBit(v) - msbPoly;
                v ^= (poly << shift);
            }
            return v;
        }

        private static int HighestBit(int x)
        {
            int hb = -1;
            while (x != 0) { x >>= 1; hb++; }
            return hb;
        }

        // -------------------- Penalty scoring (mask selection) --------------------

        private static int PenaltyScore(bool[,] m)
        {
            int size = m.GetLength(0);
            int penalty = 0;

            // N1: runs of 5+ in rows/cols
            for (int r = 0; r < size; r++)
            {
                penalty += RunPenalty(GetRow(m, r));
            }
            for (int c = 0; c < size; c++)
            {
                penalty += RunPenalty(GetCol(m, c));
            }

            // N2: 2x2 blocks
            for (int r = 0; r < size - 1; r++)
            for (int c = 0; c < size - 1; c++)
            {
                bool v = m[r, c];
                if (m[r, c + 1] == v && m[r + 1, c] == v && m[r + 1, c + 1] == v)
                    penalty += 3;
            }

            // N3: finder-like pattern in rows/cols: 10111010000 or 00001011101 (approx)
            penalty += FinderLikePenalty(m);

            // N4: balance of dark modules
            int dark = 0;
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (m[r, c]) dark++;

            int total = size * size;
            int percent = (dark * 100) / total;
            int k = Math.Abs(percent - 50) / 5;
            penalty += k * 10;

            return penalty;
        }

        private static bool[] GetRow(bool[,] m, int r)
        {
            int size = m.GetLength(0);
            var a = new bool[size];
            for (int i = 0; i < size; i++) a[i] = m[r, i];
            return a;
        }

        private static bool[] GetCol(bool[,] m, int c)
        {
            int size = m.GetLength(0);
            var a = new bool[size];
            for (int i = 0; i < size; i++) a[i] = m[i, c];
            return a;
        }

        private static int RunPenalty(bool[] line)
        {
            int p = 0;
            int run = 1;
            for (int i = 1; i < line.Length; i++)
            {
                if (line[i] == line[i - 1]) run++;
                else
                {
                    if (run >= 5) p += 3 + (run - 5);
                    run = 1;
                }
            }
            if (run >= 5) p += 3 + (run - 5);
            return p;
        }

        private static int FinderLikePenalty(bool[,] m)
        {
            int size = m.GetLength(0);
            int p = 0;

            // scan rows
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c <= size - 11; c++)
                {
                    // 10111010000
                    if (m[r, c] && !m[r, c + 1] && m[r, c + 2] && m[r, c + 3] && m[r, c + 4] && !m[r, c + 5] && m[r, c + 6] &&
                        !m[r, c + 7] && !m[r, c + 8] && !m[r, c + 9] && !m[r, c + 10])
                        p += 40;

                    // 00001011101
                    if (!m[r, c] && !m[r, c + 1] && !m[r, c + 2] && !m[r, c + 3] && m[r, c + 4] && !m[r, c + 5] && m[r, c + 6] &&
                        m[r, c + 7] && m[r, c + 8] && !m[r, c + 9] && m[r, c + 10])
                        p += 40;
                }
            }

            // scan cols
            for (int c = 0; c < size; c++)
            {
                for (int r = 0; r <= size - 11; r++)
                {
                    if (m[r, c] && !m[r + 1, c] && m[r + 2, c] && m[r + 3, c] && m[r + 4, c] && !m[r + 5, c] && m[r + 6, c] &&
                        !m[r + 7, c] && !m[r + 8, c] && !m[r + 9, c] && !m[r + 10, c])
                        p += 40;

                    if (!m[r, c] && !m[r + 1, c] && !m[r + 2, c] && !m[r + 3, c] && m[r + 4, c] && !m[r + 5, c] && m[r + 6, c] &&
                        m[r + 7, c] && m[r + 8, c] && !m[r + 9, c] && m[r + 10, c])
                        p += 40;
                }
            }

            return p;
        }

        // -------------------- Packing to Bytes --------------------

        private static byte[] PackModules(bool[,] modules)
        {
            int size = modules.GetLength(0);
            int bitCount = size * size;
            int byteCount = (bitCount + 7) / 8;
            var packed = new byte[byteCount];

            int bitIndex = 0;
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (modules[r, c])
                {
                    int b = bitIndex >> 3;
                    int bitInByte = 7 - (bitIndex & 7);
                    packed[b] |= (byte)(1 << bitInByte);
                }
                bitIndex++;
            }

            return packed;
        }

        // -------------------- Bit buffer & reader --------------------
        private sealed class BitBuffer
        {
            private readonly List<byte> _bytes = new List<byte>();
            private int _bitLen;

            public int BitLength => _bitLen;

            public void AppendBits(int value, int count)
            {
                if (count < 0 || count > 31) throw new ArgumentOutOfRangeException(nameof(count));
                for (int i = count - 1; i >= 0; i--)
                {
                    int bit = (value >> i) & 1;
                    AppendBit(bit != 0);
                }
            }

            private void AppendBit(bool bit)
            {
                int byteIndex = _bitLen >> 3;
                int bitInByte = 7 - (_bitLen & 7);
                if (byteIndex == _bytes.Count) _bytes.Add(0);
                if (bit) _bytes[byteIndex] |= (byte)(1 << bitInByte);
                _bitLen++;
            }

            public byte[] ToBytes()
            {
                // trims to full bytes actually used
                int byteCount = (_bitLen + 7) / 8;
                var arr = _bytes.ToArray();
                if (arr.Length == byteCount) return arr;
                var outArr = new byte[byteCount];
                Array.Copy(arr, outArr, byteCount);
                return outArr;
            }
        }

        private sealed class BitReader
        {
            private readonly byte[] _data;
            private int _bitPos;

            public BitReader(byte[] data) => _data = data ?? throw new ArgumentNullException(nameof(data));

            public int ReadBits(int count)
            {
                int v = 0;
                for (int i = 0; i < count; i++)
                {
                    int byteIndex = _bitPos >> 3;
                    int bitInByte = 7 - (_bitPos & 7);
                    int bit = ((_data[byteIndex] >> bitInByte) & 1);
                    v = (v << 1) | bit;
                    _bitPos++;
                }
                return v;
            }
        }

        // -------------------- Reed-Solomon (encode ECC only) --------------------
        private static class ReedSolomon
        {
            // GF(256) with primitive poly 0x11D
            private static readonly byte[] Exp = new byte[512];
            private static readonly byte[] Log = new byte[256];

            static ReedSolomon()
            {
                int x = 1;
                for (int i = 0; i < 255; i++)
                {
                    Exp[i] = (byte)x;
                    Log[(byte)x] = (byte)i;

                    x <<= 1;
                    if ((x & 0x100) != 0)
                        x ^= 0x11D;          // or x ^= 0x11D; then keep low 8 bits

                    x &= 0xFF;
                }
                for (int i = 255; i < 512; i++) Exp[i] = Exp[i - 255];
            }

            private static byte Mul(byte a, byte b)
            {
                if (a == 0 || b == 0) return 0;
                int la = Log[a];
                int lb = Log[b];
                return Exp[la + lb];
            }

            public static byte[] ComputeECC(byte[] data, int eccLen)
            {
                // Generator polynomial for eccLen
                byte[] gen = BuildGenerator(eccLen);

                var ecc = new byte[eccLen];
                foreach (byte d in data)
                {
                    byte factor = (byte)(d ^ ecc[0]);
                    // shift left
                    for (int i = 0; i < eccLen - 1; i++)
                        ecc[i] = ecc[i + 1];

                    ecc[eccLen - 1] = 0;

                    // add factor * gen
                    for (int i = 0; i < eccLen; i++)
                        ecc[i] ^= Mul(gen[i], factor);
                }
                return ecc;
            }

            private static byte[] BuildGenerator(int degree)
            {
                // gen(x) = (x - a^0)(x - a^1)...(x - a^(degree-1))
                // represented as coefficients for descending powers, but we’ll keep a simple list and multiply polynomials.
                var poly = new List<byte> { 1 }; // start with [1]
                for (int i = 0; i < degree; i++)
                {
                    byte aPow = Exp[i]; // a^i
                    poly = PolyMul(poly, new List<byte> { 1, aPow });
                }

                // We need gen coefficients excluding the leading 1 for the shift-register method above?
                // The method above expects gen length = eccLen, matching the poly without the x^degree term.
                // Our poly length is degree+1: [1, ..., ...]. Drop the first coefficient.
                var gen = new byte[degree];
                for (int i = 0; i < degree; i++)
                    gen[i] = poly[i + 1];
                return gen;
            }

            private static List<byte> PolyMul(List<byte> p, List<byte> q)
            {
                var outp = new byte[p.Count + q.Count - 1];
                for (int i = 0; i < p.Count; i++)
                for (int j = 0; j < q.Count; j++)
                    outp[i + j] ^= Mul(p[i], q[j]);
                return new List<byte>(outp);
            }
        }
    }
}