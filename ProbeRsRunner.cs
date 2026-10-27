using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace R_Flash
{
    /// <summary>
    /// 一个可用的调试器（probe-rs list 的一条结果）。
    /// </summary>
    internal sealed class ProbeInfo
    {
        public ProbeInfo(string displayText, string selector, bool inaccessible)
        {
            DisplayText = displayText;
            Selector = selector;
            Inaccessible = inaccessible;
        }

        /// <summary>probe-rs list 输出的原始内容。</summary>
        public string DisplayText { get; private set; }

        /// <summary>可以传给 --probe 的选择串（VID:PID[:序列号]），无法解析时为 null。</summary>
        public string Selector { get; private set; }

        /// <summary>当前进程无权访问该调试器。</summary>
        public bool Inaccessible { get; private set; }

        public override string ToString()
        {
            return DisplayText;
        }
    }

    /// <summary>
    /// 一次 probe-rs 调用的结果。
    /// </summary>
    internal sealed class ProbeRsResult
    {
        private readonly List<string> _lines = new List<string>();

        public int ExitCode { get; set; }

        public bool Success
        {
            get { return ExitCode == 0; }
        }

        public IList<string> Lines
        {
            get
            {
                lock (_lines)
                {
                    return new List<string>(_lines);
                }
            }
        }

        public string Output
        {
            get
            {
                lock (_lines)
                {
                    return string.Join(Environment.NewLine, _lines);
                }
            }
        }

        internal void Append(string line)
        {
            lock (_lines)
            {
                _lines.Add(line);
            }
        }
    }

    /// <summary>
    /// 通过命令行调用 probe-rs 完成设备枚举、擦除、烧写、校验和复位。
    /// </summary>
    internal sealed class ProbeRsRunner
    {
        private readonly string _executablePath;
        private readonly Dictionary<string, bool> _optionCache = new Dictionary<string, bool>();
        private string _helpText;

        public ProbeRsRunner(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
            {
                throw new ArgumentException("未指定 probe-rs 可执行文件路径。", "executablePath");
            }

            _executablePath = executablePath;
        }

        /// <summary>实际使用的 probe-rs 路径。</summary>
        public string ExecutablePath
        {
            get { return _executablePath; }
        }

        /// <summary>
        /// 依次从 App.config(ProbeRsPath)、环境变量 PROBE_RS_PATH、程序目录、PATH、
        /// %USERPROFILE%\.cargo\bin 中查找 probe-rs.exe。
        /// </summary>
        public static string LocateExecutable()
        {
            foreach (string candidate in GetCandidatePaths())
            {
                string resolved = ResolveCandidate(candidate);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        /// <summary>执行 probe-rs 命令，输出逐行回调给调用方。</summary>
        public async Task<ProbeRsResult> RunAsync(
            IList<string> arguments,
            Action<string> onOutput,
            CancellationToken cancellationToken)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _executablePath;
            startInfo.Arguments = BuildArguments(arguments);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
            startInfo.WorkingDirectory = Path.GetDirectoryName(_executablePath);

            ProbeRsResult result = new ProbeRsResult();
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                Task standardOutput = PumpAsync(process.StandardOutput, result, onOutput);
                Task standardError = PumpAsync(process.StandardError, result, onOutput);

                using (cancellationToken.Register(delegate
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }
                }))
                {
                    await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
                    process.WaitForExit();
                }

                cancellationToken.ThrowIfCancellationRequested();
                result.ExitCode = process.ExitCode;
            }

            return result;
        }

        /// <summary>枚举当前连接的所有调试器。</summary>
        public async Task<IList<ProbeInfo>> ListProbesAsync(
            CancellationToken cancellationToken,
            Action<string> onOutput)
        {
            ProbeRsResult result = await RunAsync(new[] { "list" }, onOutput, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                throw new InvalidOperationException(string.Format(
                    "probe-rs list 执行失败（退出码 {0}）。", result.ExitCode));
            }

            return ParseProbeList(result.Lines);
        }

        /// <summary>判断当前 probe-rs 版本是否提供某个子命令（结果会缓存）。</summary>
        public async Task<bool> SupportsSubcommandAsync(string name, CancellationToken cancellationToken)
        {
            if (_helpText == null)
            {
                ProbeRsResult result = await RunAsync(new[] { "--help" }, null, cancellationToken)
                    .ConfigureAwait(false);
                _helpText = result.Output;
            }

            int commandsIndex = _helpText.IndexOf("Commands:", StringComparison.OrdinalIgnoreCase);
            string section = commandsIndex >= 0 ? _helpText.Substring(commandsIndex) : _helpText;

            return Regex.IsMatch(section, @"(?im)^\s*" + Regex.Escape(name) + @"\b");
        }

        /// <summary>判断某个子命令的帮助里是否包含指定文本（例如某个选项名，结果会缓存）。</summary>
        public async Task<bool> HelpContainsAsync(
            string subcommand,
            string text,
            CancellationToken cancellationToken)
        {
            string key = subcommand + "|" + text;
            lock (_optionCache)
            {
                bool cached;
                if (_optionCache.TryGetValue(key, out cached))
                {
                    return cached;
                }
            }

            ProbeRsResult result = await RunAsync(new[] { subcommand, "--help" }, null, cancellationToken)
                .ConfigureAwait(false);
            bool supported = result.Output.IndexOf(text, StringComparison.Ordinal) >= 0;

            lock (_optionCache)
            {
                _optionCache[key] = supported;
            }

            return supported;
        }

        /// <summary>解析 probe-rs list 的输出。</summary>
        internal static IList<ProbeInfo> ParseProbeList(IEnumerable<string> lines)
        {
            List<ProbeInfo> probes = new List<ProbeInfo>();
            Regex entryPattern = new Regex(@"^\[(?<index>\d+)\]:\s*(?<text>.+)$");

            foreach (string rawLine in lines)
            {
                if (rawLine == null)
                {
                    continue;
                }

                string line = rawLine.Trim();
                Match match = entryPattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string text = match.Groups["text"].Value.Trim();
                bool inaccessible = text.IndexOf("inaccessible", StringComparison.OrdinalIgnoreCase) >= 0;
                probes.Add(new ProbeInfo(text, ExtractSelector(text), inaccessible));
            }

            return probes;
        }

        /// <summary>
        /// 从 “DAPLink CMSIS-DAP -- 0d28:0204:ABCD1234 (CMSIS-DAP)” 这样的描述中取出
        /// “0d28:0204:ABCD1234”。
        /// </summary>
        internal static string ExtractSelector(string displayText)
        {
            if (string.IsNullOrEmpty(displayText))
            {
                return null;
            }

            string text = displayText.Trim();

            int marker = text.LastIndexOf(" (", StringComparison.Ordinal);
            if (marker > 0)
            {
                text = text.Substring(0, marker).Trim();
            }

            int separator = text.LastIndexOf(" -- ", StringComparison.Ordinal);
            if (separator >= 0)
            {
                text = text.Substring(separator + 4).Trim();
            }

            Match ids = Regex.Match(text, @"([0-9a-fA-F]{4}):([0-9a-fA-F]{4})");
            if (!ids.Success)
            {
                return null;
            }

            string selector = ids.Groups[1].Value + ":" + ids.Groups[2].Value;
            string rest = text.Substring(ids.Index + ids.Length);

            if (rest.StartsWith("-", StringComparison.Ordinal))
            {
                Match iface = Regex.Match(rest, @"^-(\d+)");
                if (iface.Success)
                {
                    selector += "-" + iface.Groups[1].Value;
                    rest = rest.Substring(iface.Length);
                }
            }

            if (rest.StartsWith(":", StringComparison.Ordinal))
            {
                string serial = rest.Substring(1).Trim();
                if (serial.Length > 0)
                {
                    selector += ":" + serial;
                }
            }

            return selector;
        }

        private static async Task PumpAsync(StreamReader reader, ProbeRsResult result, Action<string> onOutput)
        {
            string line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                result.Append(line);
                if (onOutput != null)
                {
                    onOutput(line);
                }
            }
        }

        private static IEnumerable<string> GetCandidatePaths()
        {
            string configured = ConfigurationManager.AppSettings["ProbeRsPath"];
            if (!string.IsNullOrEmpty(configured) && configured.Trim().Length > 0)
            {
                yield return configured.Trim();
            }

            string fromEnvironment = Environment.GetEnvironmentVariable("PROBE_RS_PATH");
            if (!string.IsNullOrEmpty(fromEnvironment) && fromEnvironment.Trim().Length > 0)
            {
                yield return fromEnvironment.Trim();
            }

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            yield return Path.Combine(baseDirectory, "probe-rs.exe");
            yield return Path.Combine(baseDirectory, "tools", "probe-rs.exe");
            yield return Path.Combine(baseDirectory, "tools", "probe-rs", "probe-rs.exe");

            // 从程序目录逐级向上查找 tools 目录（bin\Debug 运行时对应项目根目录的 tools）。
            foreach (string directory in EnumerateParentDirectories(baseDirectory, 6))
            {
                yield return Path.Combine(directory, "tools", "probe-rs.exe");
                yield return Path.Combine(directory, "tools", "probe-rs", "probe-rs.exe");
            }

            // 从当前工作目录向上查找，便于在项目根目录直接运行调试。
            foreach (string directory in EnumerateParentDirectories(Environment.CurrentDirectory, 3))
            {
                yield return Path.Combine(directory, "tools", "probe-rs.exe");
                yield return Path.Combine(directory, "tools", "probe-rs", "probe-rs.exe");
                yield return Path.Combine(directory, "probe-rs.exe");
            }

            string searchPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(searchPath))
            {
                foreach (string directory in searchPath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = directory.Trim().Trim('"');
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    yield return Path.Combine(trimmed, "probe-rs.exe");
                }
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                yield return Path.Combine(userProfile, ".cargo", "bin", "probe-rs.exe");
            }
        }

        /// <summary>从指定目录开始逐级向上枚举父目录。</summary>
        private static IEnumerable<string> EnumerateParentDirectories(string startDirectory, int levels)
        {
            if (string.IsNullOrEmpty(startDirectory))
            {
                yield break;
            }

            DirectoryInfo directory = null;
            if (!TryGetDirectory(startDirectory, out directory))
            {
                yield break;
            }

            for (int level = 0; level < levels && directory != null; level++)
            {
                yield return directory.FullName;
                directory = directory.Parent;
            }
        }

        private static bool TryGetDirectory(string path, out DirectoryInfo directory)
        {
            directory = null;
            try
            {
                directory = new DirectoryInfo(path);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string ResolveCandidate(string candidate)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                return null;
            }

            try
            {
                string path = candidate;
                if (Directory.Exists(path))
                {
                    path = Path.Combine(path, "probe-rs.exe");
                }

                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }

                if (!Path.HasExtension(path) && File.Exists(path + ".exe"))
                {
                    return Path.GetFullPath(path + ".exe");
                }
            }
            catch (IOException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            return null;
        }

        private static string BuildArguments(IList<string> arguments)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(QuoteArgument(argument));
            }

            return builder.ToString();
        }

        private static string QuoteArgument(string argument)
        {
            if (argument == null)
            {
                return "\"\"";
            }

            if (argument.Length == 0)
            {
                return "\"\"";
            }

            bool needsQuotes = argument.IndexOf(' ') >= 0
                || argument.IndexOf('\t') >= 0
                || argument.IndexOf('"') >= 0;

            if (!needsQuotes)
            {
                return argument;
            }

            StringBuilder builder = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                {
                    builder.Append('\\', (backslashes * 2) + 1);
                    builder.Append('"');
                    backslashes = 0;
                    continue;
                }

                if (backslashes > 0)
                {
                    builder.Append('\\', backslashes);
                    backslashes = 0;
                }

                builder.Append(character);
            }

            if (backslashes > 0)
            {
                builder.Append('\\', backslashes * 2);
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
