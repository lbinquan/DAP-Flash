using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Be.Windows.Forms;

namespace R_Flash
{
    public partial class Form1 : Form
    {
        private const int DefaultSpeedKhz = 1000;

        /// <summary>SCB->CPUID 寄存器地址，读取它即可确认与芯片建立了调试连接。</summary>
        private const string CpuIdAddress = "0xE000ED00";

        private const string EraseModeAutomatic = "自动";
        private const string EraseModeChip = "全部";
        private const string EraseModeSector = "扇区";

        private ProbeRsRunner _probeRs;
        private McuLibrary _library;
        private IntelHexImage _hexImage;
        private string _hexFilePath;
        private CancellationTokenSource _cancellation;
        private bool _busy;

        // 布局基准值（单位：设计时的像素），实际使用时会按窗口缩放比例换算。
        private const float LayoutReferenceGroupWidth = 341f;
        private const int SplitterThicknessBase = 8;
        private const int HexMinimumHeightBase = 120;
        private const int LogMinimumHeightBase = 80;
        private const int LogDefaultHeightBase = 236;
        private const int RightMarginBase = 4;
        private const int ContentGapBase = 6;
        private const int FileRowGapBase = 14;
        private const int MinimumWindowWidthBase = 900;
        private const int MinimumWindowHeightBase = 520;

        private float _layoutScale;
        private int _logHeight;
        private bool _draggingSplitter;
        private int _dragStartScreenY;
        private int _dragStartLogHeight;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            InitializeLayout();
            InitializeInputs();
            LoadMcuLibrary();
            LocateProbeRs();

            // 打开窗体时自动刷新一次调试器列表。
            if (_probeRs != null)
            {
                await RunOperationAsync("刷新调试器列表", RefreshProbeListAsync, false);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CancelRunningOperation();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        #region 布局

        private void InitializeLayout()
        {
            // groupBox1 不参与重排，用它（设计宽度 341）反推 WinForms 的自动缩放比例。
            _layoutScale = groupBox1.Width > 0 ? groupBox1.Width / LayoutReferenceGroupWidth : 1f;

            if (_layoutScale < 0.5f || _layoutScale > 4f)
            {
                _layoutScale = 1f;
            }

            _logHeight = Scale(LogDefaultHeightBase);

            MinimumSize = new Size(Scale(MinimumWindowWidthBase), Scale(MinimumWindowHeightBase));

            panel_splitter.MouseDown += panel_splitter_MouseDown;
            panel_splitter.MouseMove += panel_splitter_MouseMove;
            panel_splitter.MouseUp += panel_splitter_MouseUp;
            Resize += Form1_Resize;

            LayoutPanels();
        }

        private int Scale(int value)
        {
            return (int)Math.Round(value * _layoutScale);
        }

        /// <summary>
        /// 排列右侧区域：hex 视图优先占用空间，日志区保持用户设定的高度（可用分隔条拖动调整）。
        /// groupBox1 的位置和大小始终不变。
        /// </summary>
        private void LayoutPanels()
        {
            if (_layoutScale <= 0f)
            {
                return;
            }

            int contentGap = Scale(ContentGapBase);
            int splitterThickness = Scale(SplitterThicknessBase);
            int hexMinimum = Scale(HexMinimumHeightBase);
            int logMinimum = Scale(LogMinimumHeightBase);

            int left = groupBox1.Right + contentGap;
            int right = ClientSize.Width - Scale(RightMarginBase);
            if (right - left < Scale(200))
            {
                right = left + Scale(200);
            }

            int contentWidth = right - left;

            // 顶部一行：文件路径随窗口变宽，“打开文件”按钮靠右。
            button_open_file.Left = right - button_open_file.Width;
            textBox_file_path.Left = left;
            textBox_file_path.Width = Math.Max(Scale(120), button_open_file.Left - contentGap - left);

            int hexTop = textBox_file_path.Bottom + Scale(FileRowGapBase);
            int bottom = ClientSize.Height - Scale(RightMarginBase);
            int availableHeight = bottom - hexTop;

            // 日志区优先保持用户拖出来的高度；空间不足时先压缩日志区，再压缩 hex 视图。
            int logHeight = Math.Max(_logHeight, logMinimum);
            int maxLogHeight = availableHeight - splitterThickness - hexMinimum;
            if (logHeight > maxLogHeight)
            {
                logHeight = Math.Max(logMinimum, maxLogHeight);
            }

            int logTop = bottom - logHeight;
            int splitterTop = logTop - splitterThickness;
            int hexHeight = splitterTop - hexTop;

            if (hexHeight < hexMinimum)
            {
                // 窗口太矮：先保证 hex 视图的最小高度。
                hexHeight = hexMinimum;
                splitterTop = hexTop + hexHeight;
                logTop = splitterTop + splitterThickness;
                logHeight = Math.Max(logMinimum, bottom - logTop);
            }

            hexBox_hex_data.SetBounds(left, hexTop, contentWidth, hexHeight);
            panel_splitter.SetBounds(left, splitterTop, contentWidth, splitterThickness);
            textBox_log.SetBounds(left, logTop, contentWidth, logHeight);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            LayoutPanels();
        }

        private void panel_splitter_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _draggingSplitter = true;
            _dragStartScreenY = Control.MousePosition.Y;
            _dragStartLogHeight = textBox_log.Height;
            panel_splitter.Capture = true;
        }

        private void panel_splitter_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_draggingSplitter)
            {
                return;
            }

            // 用屏幕坐标计算，避免分隔条自身移动导致的位置反馈抖动。
            int delta = _dragStartScreenY - Control.MousePosition.Y;
            int logHeight = _dragStartLogHeight + delta;

            int splitterThickness = Scale(SplitterThicknessBase);
            int logMinimum = Scale(LogMinimumHeightBase);
            int hexMinimum = Scale(HexMinimumHeightBase);
            int maxLogHeight = textBox_log.Bottom - hexBox_hex_data.Top - splitterThickness - hexMinimum;

            if (logHeight < logMinimum)
            {
                logHeight = logMinimum;
            }

            if (maxLogHeight < logMinimum)
            {
                logHeight = Math.Max(Scale(20), maxLogHeight);
            }
            else if (logHeight > maxLogHeight)
            {
                logHeight = maxLogHeight;
            }

            _logHeight = logHeight;
            LayoutPanels();
        }

        private void panel_splitter_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_draggingSplitter)
            {
                return;
            }

            _draggingSplitter = false;
            panel_splitter.Capture = false;
        }

        #endregion

        #region 初始化

        private void InitializeInputs()
        {
            comboBox_device_list.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_speed.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_mcu_group.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_mcu.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_erase_mode.DropDownStyle = ComboBoxStyle.DropDownList;

            comboBox_speed.Items.Clear();
            comboBox_speed.Items.AddRange(new object[] { "100", "200", "500", "1000", "5M", "10M" });
            comboBox_speed.SelectedItem = DefaultSpeedKhz.ToString(CultureInfo.InvariantCulture);

            comboBox_erase_mode.Items.Clear();
            comboBox_erase_mode.Items.AddRange(new object[] { EraseModeAutomatic, EraseModeChip, EraseModeSector });
            comboBox_erase_mode.SelectedIndex = 0;

            // 支持把 hex 文件直接拖到 hex 视图上打开。
            hexBox_hex_data.AllowDrop = true;
            hexBox_hex_data.DragEnter += hexBox_hex_data_DragEnter;
            hexBox_hex_data.DragDrop += hexBox_hex_data_DragDrop;
        }

        private void LoadMcuLibrary()
        {
            string directory = McuLibrary.FindYamlDirectory();
            if (directory == null)
            {
                LogError("未找到 yaml 文件夹，请把 yaml 目录放在程序目录下。");
                return;
            }

            _library = McuLibrary.Load(directory);

            comboBox_mcu_group.Items.Clear();
            foreach (McuSeries series in _library.Series)
            {
                comboBox_mcu_group.Items.Add(series);
            }

            foreach (string warning in _library.Warnings)
            {
                LogError("跳过无法解析的文件 " + warning);
            }

            Log(string.Format(
                "已从 {0} 加载 {1} 个 MCU 系列文件。", directory, _library.Series.Count));

            if (comboBox_mcu_group.Items.Count > 0)
            {
                comboBox_mcu_group.SelectedIndex = 0;
            }
            else
            {
                LogError("yaml 文件夹中没有可用的 MCU 描述文件。");
            }
        }

        private void LocateProbeRs()
        {
            string executable = ProbeRsRunner.LocateExecutable();
            if (executable == null)
            {
                _probeRs = null;
                LogError("未找到 probe-rs.exe。请把 probe-rs.exe 放到程序的 tools 目录，或用环境变量 PROBE_RS_PATH / App.config 的 ProbeRsPath 指定路径。");
                return;
            }

            _probeRs = new ProbeRsRunner(executable);
            Log("使用 probe-rs：" + executable);
        }

        #endregion

        #region 选择联动

        private void comboBox_mcu_group_SelectedIndexChanged(object sender, EventArgs e)
        {
            McuSeries series = comboBox_mcu_group.SelectedItem as McuSeries;
            comboBox_mcu.Items.Clear();

            if (series == null)
            {
                return;
            }

            foreach (string variant in series.Variants)
            {
                comboBox_mcu.Items.Add(variant);
            }

            Log(string.Format(
                "系列 {0}（{1}）包含 {2} 个型号。",
                series.SeriesName ?? Path.GetFileNameWithoutExtension(series.FilePath),
                Path.GetFileName(series.FilePath),
                series.Variants.Count));

            if (comboBox_mcu.Items.Count > 0)
            {
                comboBox_mcu.SelectedIndex = 0;
            }
        }

        private void comboBox_mcu_SelectedIndexChanged(object sender, EventArgs e)
        {
            string variant = comboBox_mcu.SelectedItem as string;
            if (!string.IsNullOrEmpty(variant))
            {
                Log("当前选择 MCU 型号：" + variant);
            }
        }

        #endregion

        #region 按钮事件

        private async void button_refresh_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("刷新调试器列表", RefreshProbeListAsync, false);
        }

        private async void button_connect_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("连接芯片", ConnectChipAsync, false);
        }

        private void button_open_file_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "打开要烧录的 hex 文件";
                dialog.Filter = "Hex 文件 (*.hex)|*.hex|所有文件 (*.*)|*.*";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                LoadHexFile(dialog.FileName);
            }
        }

        private async void button_burn_one_click_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("一键烧录", OneClickFlashAsync, true);
        }

        private async void button_earse_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("擦除 Flash", EraseFlashAsync, true);
        }

        private async void button_write_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("写入", WriteFlashOnlyAsync, true);
        }

        private async void button_check_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("校验", CheckFlashAsync, true);
        }

        private async void button_reset_Click(object sender, EventArgs e)
        {
            await RunOperationAsync("复位", ResetChipAsync, true);
        }

        #endregion

        #region hex 文件

        private void hexBox_hex_data_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = GetDroppedHexFile(e.Data) == null
                ? DragDropEffects.None
                : DragDropEffects.Copy;
        }

        private void hexBox_hex_data_DragDrop(object sender, DragEventArgs e)
        {
            string path = GetDroppedHexFile(e.Data);
            if (path == null)
            {
                return;
            }

            Log("拖入文件：" + path);
            LoadHexFile(path);
        }

        /// <summary>从拖放数据中取出第一个 .hex 文件路径，没有则返回 null。</summary>
        private static string GetDroppedHexFile(IDataObject data)
        {
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop))
            {
                return null;
            }

            string[] files = data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
            {
                return null;
            }

            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file))
                {
                    continue;
                }

                if (string.Equals(Path.GetExtension(file), ".hex", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }

            return null;
        }

        private void LoadHexFile(string path)
        {
            try
            {
                IntelHexImage image = IntelHexImage.Load(path);

                _hexImage = image;
                _hexFilePath = path;
                textBox_file_path.Text = path;

                ShowHexImage(image);

                Log(string.Format(
                    "已加载 {0}：{1} 字节，地址范围 0x{2:X8} - 0x{3:X8}。",
                    Path.GetFileName(path),
                    image.ByteCount,
                    image.StartAddress,
                    image.EndAddress - 1));

                if (image.Chunks.Count > 1)
                {
                    Log(string.Format("文件包含 {0} 段不连续的数据：", image.Chunks.Count));
                    foreach (HexChunk chunk in image.Chunks)
                    {
                        Log(string.Format(
                            "  0x{0:X8} - 0x{1:X8}（{2} 字节）", chunk.Address, chunk.EndAddress - 1, chunk.Data.Length));
                    }
                }
            }
            catch (Exception ex)
            {
                _hexImage = null;
                _hexFilePath = null;
                textBox_file_path.Text = string.Empty;
                hexBox_hex_data.ByteProvider = null;

                LogError("打开 hex 文件失败：" + ex.Message);
                MessageBox.Show(this, ex.Message, "打开文件失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowHexImage(IntelHexImage image)
        {
            long startAddress;
            bool truncated;
            byte[] buffer = image.ToDisplayBuffer(out startAddress, out truncated);

            hexBox_hex_data.ByteProvider = new DynamicByteProvider(buffer);
            hexBox_hex_data.LineInfoOffset = startAddress;

            if (truncated)
            {
                LogError(string.Format(
                    "数据地址跨度超过 {0} MB，hex 视图只显示第一段数据。", IntelHexImage.MaxDisplayBytes / (1024 * 1024)));
            }
        }

        #endregion

        #region probe-rs 操作

        private async Task RefreshProbeListAsync(CancellationToken cancellationToken)
        {
            EnsureProbeRs();

            comboBox_device_list.Items.Clear();
            Log("正在扫描已连接的调试器 ...");

            IList<ProbeInfo> probes = await _probeRs.ListProbesAsync(cancellationToken, Log);

            foreach (ProbeInfo probe in probes)
            {
                comboBox_device_list.Items.Add(probe);
            }

            if (probes.Count == 0)
            {
                LogError("没有找到调试器，请检查 DAP-Link 是否已连接。");
                return;
            }

            comboBox_device_list.SelectedIndex = 0;
            Log(string.Format("找到 {0} 个调试器。", probes.Count));
        }

        private async Task ConnectChipAsync(CancellationToken cancellationToken)
        {
            EnsureProbeRs();

            ProbeInfo probe = GetSelectedProbe();
            if (probe == null)
            {
                throw new InvalidOperationException("请先刷新并选择一个调试器。");
            }

            McuSeries series = comboBox_mcu_group.SelectedItem as McuSeries;
            string variant = comboBox_mcu.SelectedItem as string;

            if (series == null || string.IsNullOrEmpty(variant))
            {
                // 没有选择 MCU 型号时，只能探测调试器与目标调试端口。
                List<string> infoArguments = new List<string> { "info" };
                AppendProbeArguments(infoArguments, probe);
                await ExecuteStepAsync("连接芯片（探测调试端口）", infoArguments, cancellationToken);
                return;
            }

            await ConnectTargetAsync(probe, series, variant, cancellationToken);
        }

        /// <summary>
        /// 按所选 MCU 读取内核 CPUID 寄存器来确认连接（只读操作，不会改动芯片内容）。
        /// </summary>
        private async Task ConnectTargetAsync(
            ProbeInfo probe,
            McuSeries series,
            string variant,
            CancellationToken cancellationToken)
        {
            if (!await _probeRs.SupportsSubcommandAsync("read", cancellationToken))
            {
                // 老版本 probe-rs 没有 read 子命令，退回 info 探测。
                List<string> infoArguments = new List<string> { "info" };
                AppendProbeArguments(infoArguments, probe);
                await ExecuteStepAsync("连接芯片", infoArguments, cancellationToken);
                return;
            }

            List<string> arguments = new List<string> { "read", "b32", CpuIdAddress, "1" };
            AppendTargetArguments(arguments, series, variant, probe, GetSpeedKhz());

            ProbeRsResult result = await ExecuteStepAsync("连接芯片", arguments, cancellationToken);

            string cpuId = ExtractCpuId(result.Lines);
            Log(cpuId == null
                ? "已与芯片建立调试连接。"
                : "芯片内核：" + DescribeCpuId(cpuId));
        }

        /// <summary>一次烧录操作需要的上下文（调试器、芯片、速度、擦除方式）。</summary>
        private sealed class FlashTarget
        {
            public ProbeInfo Probe { get; set; }

            public McuSeries Series { get; set; }

            public string Variant { get; set; }

            public int SpeedKhz { get; set; }

            public string EraseMode { get; set; }
        }

        /// <summary>收集并校验界面上的参数。</summary>
        private FlashTarget CreateFlashTarget(bool requireHexFile)
        {
            EnsureProbeRs();

            FlashTarget target = new FlashTarget();
            target.Probe = GetSelectedProbe();
            target.Series = comboBox_mcu_group.SelectedItem as McuSeries;
            target.Variant = comboBox_mcu.SelectedItem as string;
            target.SpeedKhz = GetSpeedKhz();
            target.EraseMode = GetEraseMode();

            if (target.Probe == null)
            {
                throw new InvalidOperationException("请先刷新并选择一个调试器。");
            }

            if (target.Series == null || string.IsNullOrEmpty(target.Variant))
            {
                throw new InvalidOperationException("请先选择芯片系列和芯片型号。");
            }

            if (requireHexFile && (_hexImage == null || string.IsNullOrEmpty(_hexFilePath)))
            {
                throw new InvalidOperationException("请先通过“打开文件”选择要烧录的 hex 文件。");
            }

            return target;
        }

        private void LogFlashTarget(FlashTarget target)
        {
            if (_hexImage != null && !string.IsNullOrEmpty(_hexFilePath))
            {
                Log(string.Format(
                    "固件：{0}（{1} 字节，0x{2:X8} - 0x{3:X8}）",
                    Path.GetFileName(_hexFilePath),
                    _hexImage.ByteCount,
                    _hexImage.StartAddress,
                    _hexImage.EndAddress - 1));
            }

            Log(string.Format(
                "目标：{0} / {1}（{2}）",
                target.Series.SeriesName ?? Path.GetFileNameWithoutExtension(target.Series.FilePath),
                target.Variant,
                Path.GetFileName(target.Series.FilePath)));
            Log("调试器：" + target.Probe.DisplayText);
            Log(string.Format("速度：{0} kHz，擦除方式：{1}", target.SpeedKhz, target.EraseMode));
        }

        /// <summary>整片擦除（probe-rs erase）。</summary>
        private async Task EraseChipAsync(FlashTarget target, CancellationToken cancellationToken)
        {
            List<string> arguments = new List<string> { "erase" };
            AppendTargetArguments(arguments, target.Series, target.Variant, target.Probe, target.SpeedKhz);
            arguments.Add("--disable-progressbars");

            await ExecuteStepAsync("擦除 Flash（整片）", arguments, cancellationToken);
        }

        /// <summary>写入固件：擦除方式为“全部”时先整片擦除，其余方式由 probe-rs 按需擦除。</summary>
        private async Task WriteFlashAsync(FlashTarget target, CancellationToken cancellationToken)
        {
            bool verifySupported = await _probeRs.SupportsSubcommandAsync("verify", cancellationToken);
            bool restoreUnwritten = false;

            if (target.EraseMode == EraseModeChip)
            {
                await EraseChipAsync(target, cancellationToken);
            }
            else if (target.EraseMode == EraseModeSector)
            {
                restoreUnwritten = await _probeRs.HelpContainsAsync("download", "--restore-unwritten", cancellationToken);
                Log(restoreUnwritten
                    ? "--- 擦除：仅擦除固件覆盖的扇区，并保留扇区内未被固件覆盖的内容。"
                    : "--- 擦除：仅擦除固件覆盖的扇区，由写入步骤按需完成。");
            }
            else
            {
                Log("--- 擦除：自动，由 probe-rs 根据固件范围决定擦除方式。");
            }

            List<string> arguments = new List<string> { "download" };
            AppendTargetArguments(arguments, target.Series, target.Variant, target.Probe, target.SpeedKhz);
            arguments.Add("--disable-progressbars");
            if (restoreUnwritten)
            {
                arguments.Add("--restore-unwritten");
            }

            if (!verifySupported)
            {
                // 旧版本没有 verify 子命令时，在写入过程中完成校验。
                arguments.Add("--verify");
            }

            await AppendImageFormatArgumentsAsync(arguments, "download", cancellationToken);
            arguments.Add(_hexFilePath);

            ProbeRsResult result = await ExecuteStepAsync("写入", arguments, cancellationToken, false);
            if (ContainsVerificationFailure(result.Lines))
            {
                throw new InvalidOperationException("写入后的校验失败：芯片内容与文件不一致。");
            }

            Log("--- 写入完成。");
        }

        /// <summary>校验芯片内容与 hex 文件是否一致。</summary>
        private async Task VerifyFlashAsync(FlashTarget target, CancellationToken cancellationToken)
        {
            if (!await _probeRs.SupportsSubcommandAsync("verify", cancellationToken))
            {
                Log("--- 校验：当前 probe-rs 没有 verify 子命令，只能在写入时通过 download --verify 校验。");
                return;
            }

            List<string> arguments = new List<string> { "verify" };
            AppendTargetArguments(arguments, target.Series, target.Variant, target.Probe, target.SpeedKhz);
            arguments.Add("--disable-progressbars");
            await AppendImageFormatArgumentsAsync(arguments, "verify", cancellationToken);
            arguments.Add(_hexFilePath);

            ProbeRsResult result = await ExecuteStepAsync("校验", arguments, cancellationToken, false);

            // probe-rs 0.32 的 verify 在校验不一致时退出码仍为 0，必须检查输出内容。
            if (ContainsVerificationFailure(result.Lines))
            {
                throw new InvalidOperationException("校验失败：芯片中的内容与文件不一致。");
            }

            Log("--- 校验完成。");
        }

        /// <summary>复位芯片。</summary>
        private async Task ResetTargetAsync(FlashTarget target, CancellationToken cancellationToken)
        {
            List<string> arguments = new List<string> { "reset" };
            AppendTargetArguments(arguments, target.Series, target.Variant, target.Probe, target.SpeedKhz);

            await ExecuteStepAsync("复位", arguments, cancellationToken);
        }

        private async Task OneClickFlashAsync(CancellationToken cancellationToken)
        {
            FlashTarget target = CreateFlashTarget(true);

            Log("================ 开始一键烧录 ================");
            LogFlashTarget(target);

            // 1. 连接芯片
            await ConnectTargetAsync(target.Probe, target.Series, target.Variant, cancellationToken);

            // 2. 擦除 + 3. 写入
            await WriteFlashAsync(target, cancellationToken);

            // 4. 校验
            await VerifyFlashAsync(target, cancellationToken);

            // 5. 复位
            await ResetTargetAsync(target, cancellationToken);

            Log("================ 一键烧录完成 ================");
        }

        private async Task EraseFlashAsync(CancellationToken cancellationToken)
        {
            FlashTarget target = CreateFlashTarget(false);

            Log("================ 开始擦除 Flash ================");
            LogFlashTarget(target);

            if (target.EraseMode != EraseModeChip)
            {
                Log("--- 提示：probe-rs 的 erase 命令只支持整片擦除；选择“扇区”时，扇区擦除会在一键烧录/写入时按需进行。");
            }

            await EraseChipAsync(target, cancellationToken);

            Log("================ 擦除完成 ================");
        }

        private async Task WriteFlashOnlyAsync(CancellationToken cancellationToken)
        {
            FlashTarget target = CreateFlashTarget(true);

            Log("================ 开始写入 ================");
            LogFlashTarget(target);

            await WriteFlashAsync(target, cancellationToken);

            Log("================ 写入完成 ================");
        }

        private async Task CheckFlashAsync(CancellationToken cancellationToken)
        {
            FlashTarget target = CreateFlashTarget(true);

            Log("================ 开始校验 ================");
            LogFlashTarget(target);

            await VerifyFlashAsync(target, cancellationToken);

            Log("================ 校验完成 ================");
        }

        private async Task ResetChipAsync(CancellationToken cancellationToken)
        {
            FlashTarget target = CreateFlashTarget(false);

            Log("================ 开始复位 ================");
            LogFlashTarget(target);

            await ResetTargetAsync(target, cancellationToken);

            Log("================ 复位完成 ================");
        }

        private async Task<ProbeRsResult> ExecuteStepAsync(
            string title,
            IList<string> arguments,
            CancellationToken cancellationToken,
            bool logCompletion = true)
        {
            Log(string.Format("--- {0}：probe-rs {1}", title, string.Join(" ", arguments)));

            ProbeRsResult result = await _probeRs.RunAsync(arguments, Log, cancellationToken);
            if (!result.Success)
            {
                throw new InvalidOperationException(string.Format(
                    "{0}失败（退出码 {1}），请查看日志中的 probe-rs 输出。", title, result.ExitCode));
            }

            if (logCompletion)
            {
                Log(string.Format("--- {0}完成。", title));
            }

            return result;
        }

        #endregion

        #region 参数组装

        private void AppendProbeArguments(List<string> arguments, ProbeInfo probe)
        {
            arguments.Add("--protocol");
            arguments.Add("swd");
            arguments.Add("--speed");
            arguments.Add(GetSpeedKhz().ToString(CultureInfo.InvariantCulture));
            arguments.Add("--non-interactive");

            if (probe != null && !string.IsNullOrEmpty(probe.Selector))
            {
                arguments.Add("--probe");
                arguments.Add(probe.Selector);
            }
        }

        private void AppendTargetArguments(
            List<string> arguments,
            McuSeries series,
            string variant,
            ProbeInfo probe,
            int speedKhz)
        {
            arguments.Add("--chip");
            arguments.Add(variant);
            arguments.Add("--chip-description-path");
            arguments.Add(series.FilePath);
            arguments.Add("--protocol");
            arguments.Add("swd");
            arguments.Add("--speed");
            arguments.Add(speedKhz.ToString(CultureInfo.InvariantCulture));
            arguments.Add("--non-interactive");

            if (probe != null && !string.IsNullOrEmpty(probe.Selector))
            {
                arguments.Add("--probe");
                arguments.Add(probe.Selector);
            }
        }

        /// <summary>
        /// 声明镜像文件格式。probe-rs 0.32+ 的 --binary-format 默认为 target（按芯片偏好，通常是 ELF），
        /// 不会根据 .hex 扩展名推断，必须显式指定，否则会按 ELF 解析失败。
        /// </summary>
        private async Task AppendImageFormatArgumentsAsync(
            List<string> arguments,
            string subcommand,
            CancellationToken cancellationToken)
        {
            if (await _probeRs.HelpContainsAsync(subcommand, "--binary-format", cancellationToken))
            {
                arguments.Add("--binary-format");
                arguments.Add("hex");
                return;
            }

            // 老版本 probe-rs 使用 --format，且 hex 的取值名为 ihex。
            if (await _probeRs.HelpContainsAsync(subcommand, "--format", cancellationToken))
            {
                bool olderName = await _probeRs.HelpContainsAsync(subcommand, "ihex", cancellationToken);
                arguments.Add("--format");
                arguments.Add(olderName ? "ihex" : "hex");
            }
        }

        private ProbeInfo GetSelectedProbe()
        {
            return comboBox_device_list.SelectedItem as ProbeInfo;
        }

        private int GetSpeedKhz()
        {
            return ParseSpeedKhz(comboBox_speed.Text);
        }

        /// <summary>
        /// 把速度选项转换成 probe-rs 需要的 kHz 数值，支持 “200”、“1000”、“5M”、“10M” 这类写法。
        /// </summary>
        private static int ParseSpeedKhz(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return DefaultSpeedKhz;
            }

            string value = text.Trim();
            double multiplier = 1d;

            if (value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1000d;
                value = value.Substring(0, value.Length - 1);
            }
            else if (value.EndsWith("K", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 1);
            }

            double number;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || number <= 0d)
            {
                return DefaultSpeedKhz;
            }

            return (int)Math.Round(number * multiplier);
        }

        private string GetEraseMode()
        {
            string mode = comboBox_erase_mode.SelectedItem as string;
            return string.IsNullOrEmpty(mode) ? EraseModeAutomatic : mode;
        }

        private void EnsureProbeRs()
        {
            if (_probeRs == null)
            {
                throw new InvalidOperationException(
                    "未找到 probe-rs.exe。请把 probe-rs.exe 放到程序的 tools 目录，或用环境变量 PROBE_RS_PATH / App.config 的 ProbeRsPath 指定路径。");
            }
        }

        /// <summary>判断 probe-rs 的输出里是否报告了校验不匹配。</summary>
        private static bool ContainsVerificationFailure(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (line == null)
                {
                    continue;
                }

                if (line.IndexOf("Verification failed", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("do not match", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("contents do not match", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>从 probe-rs read 的输出（形如 "e000ed00: 410fc241"）里取出 CPUID 值。</summary>
        private static string ExtractCpuId(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (line == null)
                {
                    continue;
                }

                int separator = line.IndexOf(':');
                if (separator < 0)
                {
                    continue;
                }

                string value = line.Substring(separator + 1).Trim();
                if (value.Length != 8)
                {
                    continue;
                }

                bool isHex = true;
                foreach (char character in value)
                {
                    if (!Uri.IsHexDigit(character))
                    {
                        isHex = false;
                        break;
                    }
                }

                if (isHex)
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>把 SCB->CPUID 解析成内核型号与修订号。</summary>
        private static string DescribeCpuId(string cpuId)
        {
            uint value;
            if (!uint.TryParse(cpuId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                return "0x" + cpuId.ToUpperInvariant();
            }

            int partNumber = (int)((value >> 4) & 0xFFF);
            int variant = (int)((value >> 20) & 0xF);
            int revision = (int)(value & 0xF);

            string core;
            switch (partNumber)
            {
                case 0xC20:
                    core = "Cortex-M0";
                    break;
                case 0xC60:
                    core = "Cortex-M0+";
                    break;
                case 0xC23:
                    core = "Cortex-M3";
                    break;
                case 0xC24:
                    core = "Cortex-M4";
                    break;
                case 0xC27:
                    core = "Cortex-M7";
                    break;
                case 0xD20:
                    core = "Cortex-M23";
                    break;
                case 0xD21:
                    core = "Cortex-M33";
                    break;
                case 0xD22:
                    core = "Cortex-M55";
                    break;
                case 0xD23:
                    core = "Cortex-M85";
                    break;
                default:
                    core = string.Format("未知内核（PARTNO 0x{0:X3}）", partNumber);
                    break;
            }

            return string.Format("0x{0}（{1} r{2}p{3}）", cpuId.ToUpperInvariant(), core, variant, revision);
        }

        #endregion

        #region 通用辅助

        private async Task RunOperationAsync(
            string name,
            Func<CancellationToken, Task> operation,
            bool showResultDialog)
        {
            if (_busy)
            {
                return;
            }

            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                _cancellation = cancellation;
                SetBusy(true);

                try
                {
                    await operation(cancellation.Token);

                    if (showResultDialog)
                    {
                        MessageBox.Show(this, name + "完成。", "DAP-Flash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log(name + "已取消。");
                }
                catch (Exception ex)
                {
                    LogError(name + "失败：" + ex.Message);

                    if (showResultDialog)
                    {
                        MessageBox.Show(this, ex.Message, name + "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    _cancellation = null;
                    SetBusy(false);
                }
            }
        }

        private void CancelRunningOperation()
        {
            CancellationTokenSource cancellation = _cancellation;
            if (cancellation == null)
            {
                return;
            }

            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;

            if (IsDisposed)
            {
                return;
            }

            button_refresh.Enabled = !busy;
            button_connect.Enabled = !busy;
            button_open_file.Enabled = !busy;
            button_burn_one_click.Enabled = !busy;
            button_earse.Enabled = !busy;
            button_write.Enabled = !busy;
            button_check.Enabled = !busy;
            button_reset.Enabled = !busy;
            comboBox_device_list.Enabled = !busy;
            comboBox_speed.Enabled = !busy;
            comboBox_mcu_group.Enabled = !busy;
            comboBox_mcu.Enabled = !busy;
            comboBox_erase_mode.Enabled = !busy;

            UseWaitCursor = busy;
        }

        private void Log(string message)
        {
            AppendLog(message, false);
        }

        private void LogError(string message)
        {
            AppendLog(message, true);
        }

        private static readonly System.Text.RegularExpressions.Regex AnsiEscapePattern =
            new System.Text.RegularExpressions.Regex(@"\x1b\[[0-9;?]*[ -/]*[@-~]", System.Text.RegularExpressions.RegexOptions.Compiled);

        private void AppendLog(string message, bool isError)
        {
            if (message == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string, bool>(AppendLog), message, isError);
                }
                catch (InvalidOperationException)
                {
                    // 窗体已经关闭，直接丢弃日志。
                }

                return;
            }

            if (textBox_log.IsDisposed)
            {
                return;
            }

            // probe-rs 在 stderr 上会输出 ANSI 颜色转义码，去掉后再显示。
            message = AnsiEscapePattern.Replace(message, string.Empty);

            string prefix = "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] ";
            if (isError)
            {
                prefix += "[错误] ";
            }

            string[] lines = message.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            StringBuilder builder = new StringBuilder();
            foreach (string line in lines)
            {
                builder.Append(prefix).Append(line).Append(Environment.NewLine);
            }

            textBox_log.AppendText(builder.ToString());
            textBox_log.SelectionStart = textBox_log.TextLength;
            textBox_log.ScrollToCaret();
        }

        #endregion

        private void label_mcu_group_Click(object sender, EventArgs e)
        {

        }
    }
}
