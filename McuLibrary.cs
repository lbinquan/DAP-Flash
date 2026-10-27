using System;
using System.Collections.Generic;
using System.IO;

namespace R_Flash
{
    /// <summary>
    /// yaml 文件夹中的一个芯片系列文件（probe-rs 的 target 描述文件）。
    /// </summary>
    internal sealed class McuSeries
    {
        public McuSeries(string filePath, string seriesName, IList<string> variants)
        {
            FilePath = filePath;
            SeriesName = seriesName;
            Variants = variants;
        }

        /// <summary>yaml 文件的完整路径。</summary>
        public string FilePath { get; private set; }

        /// <summary>yaml 文件顶层的系列名，例如 “STM32F0 Series”。</summary>
        public string SeriesName { get; private set; }

        /// <summary>系列里包含的具体型号（variants）。</summary>
        public IList<string> Variants { get; private set; }

        public override string ToString()
        {
            return Path.GetFileName(FilePath);
        }
    }

    /// <summary>
    /// 读取 yaml 文件夹，提供系列与型号列表。
    /// </summary>
    internal sealed class McuLibrary
    {
        private readonly List<McuSeries> _series = new List<McuSeries>();
        private readonly List<string> _warnings = new List<string>();

        private McuLibrary()
        {
        }

        /// <summary>解析到的所有系列。</summary>
        public IList<McuSeries> Series
        {
            get { return _series; }
        }

        /// <summary>解析过程中跳过的文件及原因。</summary>
        public IList<string> Warnings
        {
            get { return _warnings; }
        }

        /// <summary>
        /// 加载指定目录下所有 .yaml/.yml 文件。
        /// </summary>
        public static McuLibrary Load(string directory)
        {
            McuLibrary library = new McuLibrary();
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return library;
            }

            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(directory, "*.yaml"));
            files.AddRange(Directory.GetFiles(directory, "*.yml"));
            files.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (string file in files)
            {
                try
                {
                    library._series.Add(Parse(file));
                }
                catch (Exception ex)
                {
                    library._warnings.Add(string.Format("{0}：{1}", Path.GetFileName(file), ex.Message));
                }
            }

            return library;
        }

        /// <summary>
        /// 从程序目录（以及当前工作目录）向上查找 yaml 文件夹（bin\Debug 运行时需要回到项目根目录）。
        /// </summary>
        public static string FindYamlDirectory()
        {
            string fromBaseDirectory = SearchYamlDirectory(AppDomain.CurrentDomain.BaseDirectory, 6);
            if (fromBaseDirectory != null)
            {
                return fromBaseDirectory;
            }

            return SearchYamlDirectory(Environment.CurrentDirectory, 3);
        }

        private static string SearchYamlDirectory(string startDirectory, int levels)
        {
            if (string.IsNullOrEmpty(startDirectory))
            {
                return null;
            }

            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(startDirectory);
            }
            catch (ArgumentException)
            {
                return null;
            }

            for (int level = 0; level < levels && directory != null; level++)
            {
                string candidate = Path.Combine(directory.FullName, "yaml");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static McuSeries Parse(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            string seriesName = ParseSeriesName(lines);
            List<string> variants = ParseVariants(lines);

            if (variants.Count == 0 && !string.IsNullOrEmpty(seriesName))
            {
                // 单芯片的 target 描述文件没有 variants，直接用系列名作为型号。
                variants.Add(seriesName);
            }

            if (variants.Count == 0)
            {
                throw new FormatException("文件中没有找到任何 MCU 型号（variants）。");
            }

            return new McuSeries(filePath, seriesName, variants);
        }

        private static string ParseSeriesName(string[] lines)
        {
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                if (GetIndent(line) == 0 && line.StartsWith("name:", StringComparison.Ordinal))
                {
                    return Unquote(line.Substring("name:".Length).Trim());
                }
            }

            return null;
        }

        private static List<string> ParseVariants(string[] lines)
        {
            List<string> variants = new List<string>();

            int variantsLine = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Length == 0)
                {
                    continue;
                }

                if (GetIndent(lines[i]) == 0 && lines[i].StartsWith("variants:", StringComparison.Ordinal))
                {
                    variantsLine = i;
                    break;
                }
            }

            if (variantsLine < 0)
            {
                return variants;
            }

            int itemIndent = -1;
            for (int i = variantsLine + 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                int indent = GetIndent(line);
                string content = line.Substring(indent);

                if (content[0] == '-')
                {
                    if (itemIndent < 0)
                    {
                        itemIndent = indent;
                    }

                    if (indent == itemIndent)
                    {
                        string item = content.Substring(1).Trim();
                        if (item.StartsWith("name:", StringComparison.Ordinal))
                        {
                            string name = Unquote(item.Substring("name:".Length).Trim());
                            if (name.Length > 0)
                            {
                                variants.Add(name);
                            }
                        }
                    }

                    continue;
                }

                // 遇到与 variants 同级的键，说明 variants 段结束。
                if (itemIndent >= 0 && indent <= itemIndent)
                {
                    break;
                }
            }

            return variants;
        }

        private static int GetIndent(string line)
        {
            int indent = 0;
            while (indent < line.Length && line[indent] == ' ')
            {
                indent++;
            }

            return indent;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2)
            {
                char first = value[0];
                char last = value[value.Length - 1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                {
                    return value.Substring(1, value.Length - 2);
                }
            }

            return value;
        }
    }
}
