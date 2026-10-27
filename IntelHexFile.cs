using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace R_Flash
{
    /// <summary>
    /// Intel HEX 文件中的一段连续数据。
    /// </summary>
    internal sealed class HexChunk
    {
        public HexChunk(long address, byte[] data)
        {
            Address = address;
            Data = data;
        }

        /// <summary>数据块的起始地址。</summary>
        public long Address { get; private set; }

        /// <summary>数据块的内容。</summary>
        public byte[] Data { get; private set; }

        /// <summary>数据块的结束地址（不包含）。</summary>
        public long EndAddress
        {
            get { return Address + Data.Length; }
        }
    }

    /// <summary>
    /// 解析后的 Intel HEX 镜像。
    /// </summary>
    internal sealed class IntelHexImage
    {
        /// <summary>HexBox 最多显示的数据长度，避免超大跨度的镜像占用过多内存。</summary>
        public const int MaxDisplayBytes = 4 * 1024 * 1024;

        private readonly List<HexChunk> _chunks;

        private IntelHexImage(List<HexChunk> chunks)
        {
            _chunks = chunks;

            StartAddress = chunks[0].Address;
            EndAddress = chunks[0].EndAddress;
            long total = 0;
            foreach (HexChunk chunk in chunks)
            {
                total += chunk.Data.Length;
                if (chunk.Address < StartAddress)
                {
                    StartAddress = chunk.Address;
                }

                if (chunk.EndAddress > EndAddress)
                {
                    EndAddress = chunk.EndAddress;
                }
            }

            ByteCount = total;
        }

        /// <summary>镜像的最小地址。</summary>
        public long StartAddress { get; private set; }

        /// <summary>镜像的结束地址（不包含）。</summary>
        public long EndAddress { get; private set; }

        /// <summary>实际数据字节数。</summary>
        public long ByteCount { get; private set; }

        /// <summary>镜像包含的数据块（按地址升序、已合并相邻块）。</summary>
        public IList<HexChunk> Chunks
        {
            get { return _chunks; }
        }

        /// <summary>
        /// 解析 Intel HEX 文件。
        /// </summary>
        public static IntelHexImage Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("未指定 hex 文件路径。", "filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("找不到 hex 文件：" + filePath, filePath);
            }

            List<HexChunk> rawChunks = new List<HexChunk>();
            long extendedAddress = 0;
            bool reachedEndOfFile = false;

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                int lineNumber = i + 1;
                byte[] record = DecodeRecord(line, lineNumber);

                int byteCount = record[0];
                int type = record[3];
                int offset = (record[1] << 8) | record[2];

                switch (type)
                {
                    case 0x00: // 数据记录
                        if (byteCount > 0)
                        {
                            byte[] data = new byte[byteCount];
                            Buffer.BlockCopy(record, 4, data, 0, byteCount);
                            rawChunks.Add(new HexChunk(extendedAddress + offset, data));
                        }

                        break;

                    case 0x01: // 文件结束
                        reachedEndOfFile = true;
                        break;

                    case 0x02: // 扩展段地址
                        extendedAddress = (long)((record[4] << 8) | record[5]) << 4;
                        break;

                    case 0x04: // 扩展线性地址
                        extendedAddress = (long)((record[4] << 8) | record[5]) << 16;
                        break;

                    case 0x03: // 起始段地址，烧录时可忽略
                    case 0x05: // 起始线性地址，烧录时可忽略
                        break;

                    default:
                        throw new FormatException(string.Format(
                            "第 {0} 行包含无法识别的 Intel HEX 记录类型 0x{1:X2}。", lineNumber, type));
                }

                if (reachedEndOfFile)
                {
                    break;
                }
            }

            if (rawChunks.Count == 0)
            {
                throw new FormatException("hex 文件中没有任何数据记录。");
            }

            return new IntelHexImage(MergeChunks(rawChunks));
        }

        /// <summary>
        /// 把镜像展开成一段连续数据，空隙用擦除值 0xFF 填充，便于在 HexBox 中显示。
        /// </summary>
        /// <param name="bufferStartAddress">返回缓冲区对应的起始地址。</param>
        /// <param name="truncated">数据跨度超过上限时为 true，此时只返回第一个数据块。</param>
        public byte[] ToDisplayBuffer(out long bufferStartAddress, out bool truncated)
        {
            truncated = false;
            bufferStartAddress = StartAddress;

            long span = EndAddress - StartAddress;
            if (span <= 0)
            {
                return new byte[0];
            }

            if (span > MaxDisplayBytes)
            {
                // 地址跨度太大（例如同时包含 Flash 与选项字节），只显示第一段数据。
                truncated = true;
                bufferStartAddress = _chunks[0].Address;
                return (byte[])_chunks[0].Data.Clone();
            }

            byte[] buffer = new byte[span];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0xFF;
            }

            foreach (HexChunk chunk in _chunks)
            {
                Buffer.BlockCopy(chunk.Data, 0, buffer, (int)(chunk.Address - StartAddress), chunk.Data.Length);
            }

            return buffer;
        }

        private static byte[] DecodeRecord(string line, int lineNumber)
        {
            if (line[0] != ':')
            {
                throw new FormatException(string.Format(
                    "第 {0} 行不是有效的 Intel HEX 记录（缺少开头的 ':'）。", lineNumber));
            }

            int hexCharCount = line.Length - 1;
            if (hexCharCount % 2 != 0)
            {
                throw new FormatException(string.Format(
                    "第 {0} 行的字符数为奇数，无法解析。", lineNumber));
            }

            byte[] record = new byte[hexCharCount / 2];
            for (int i = 0; i < record.Length; i++)
            {
                record[i] = byte.Parse(
                    line.Substring(1 + (i * 2), 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
            }

            if (record.Length < 5)
            {
                throw new FormatException(string.Format("第 {0} 行的记录长度不足。", lineNumber));
            }

            int byteCount = record[0];
            if (record.Length != byteCount + 5)
            {
                throw new FormatException(string.Format(
                    "第 {0} 行声明的数据长度为 {1} 字节，但实际为 {2} 字节。",
                    lineNumber, byteCount, record.Length - 5));
            }

            int sum = 0;
            foreach (byte value in record)
            {
                sum += value;
            }

            if ((sum & 0xFF) != 0)
            {
                throw new FormatException(string.Format("第 {0} 行的校验和错误。", lineNumber));
            }

            return record;
        }

        /// <summary>按地址排序，并合并相邻或重叠的数据块（重叠部分后写入的优先）。</summary>
        private static List<HexChunk> MergeChunks(List<HexChunk> chunks)
        {
            chunks.Sort(delegate(HexChunk left, HexChunk right)
            {
                return left.Address.CompareTo(right.Address);
            });

            List<HexChunk> merged = new List<HexChunk>();
            int index = 0;
            while (index < chunks.Count)
            {
                long start = chunks[index].Address;
                long end = chunks[index].EndAddress;

                // 找出所有与当前块相连或重叠的记录。
                int next = index + 1;
                while (next < chunks.Count && chunks[next].Address <= end)
                {
                    if (chunks[next].EndAddress > end)
                    {
                        end = chunks[next].EndAddress;
                    }

                    next++;
                }

                byte[] buffer = new byte[end - start];
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = 0xFF;
                }

                for (int i = index; i < next; i++)
                {
                    Buffer.BlockCopy(chunks[i].Data, 0, buffer, (int)(chunks[i].Address - start), chunks[i].Data.Length);
                }

                merged.Add(new HexChunk(start, buffer));
                index = next;
            }

            return merged;
        }
    }
}
