# DAP-Flash

一个基于 [probe-rs](https://probe.rs) 的 Windows 图形化烧录工具，用于给 ARM Cortex-M 芯片烧写 Intel HEX 固件。

界面简洁、免安装、不联网：芯片描述完全来自本地 `yaml/` 目录，运行过程中不会去下载 ARM 官方 pack。

## 功能

- **调试器列表**：自动枚举电脑上连接的 DAPLink / CMSIS-DAP 等调试器，支持多探针选择、一键刷新。
- **芯片选择**：芯片系列来自 `yaml/` 目录下的 target 描述文件，选定后自动列出该系列支持的所有型号。
- **速度可调**：`100 / 200 / 500 / 1000 kHz / 5M / 10M`，默认 1000 kHz。
- **三种擦除方式**：自动 / 全部 / 扇区。
- **一键烧录**：连接 → 擦除 → 写入 → 校验 → 复位，全流程自动完成并输出日志。
- **分步操作**：`擦除Flash`、`写入`、`校验`、`复位` 四个按钮可单独执行。
- **HEX 预览**：打开 `.hex` 后按解析结果展示固件二进制，地址列显示真实的 Flash 地址。
- **拖拽打开**：把 `.hex` 文件直接拖到 HEX 视图上即可载入（非 hex 文件会被拒绝）。
- **日志窗口**：带时间戳输出每一步执行的 probe-rs 命令与结果，方便定位问题。
- **可调布局**：十六进制视图与日志窗口之间的分隔条可以拖动；窗口缩放时优先放大 HEX 视图。

## 界面

窗口分为三块：

- **左侧面板（`DAP-Link`，大小固定）**：调试器下拉框 + 「刷新」按钮、速度、芯片系列、芯片型号、擦除方式、「连接芯片」，以及「擦除Flash」「写入」「校验」「复位」和右下角的大按钮「一键烧录」。
- **右上（HEX 视图）**：顶部一行是固件路径与「打开文件」按钮，下方按解析结果展示固件二进制。
- **右下（日志窗口）**：带时间戳记录每一步执行的 probe-rs 命令行与输出。

HEX 视图与日志窗口之间有一条分隔条，可以上下拖动调整两者高度；窗口放大时优先扩展 HEX 视图，日志窗口保持你拖出来的高度。左侧 `DAP-Link` 面板始终固定不变。

## 环境要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows（WinForms，.NET Framework 4.8） |
| 运行时 | [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)（Windows 10/11 通常已自带） |
| 烧录后端 | probe-rs 0.31+（已在 0.32.0 上验证） |
| 调试器 | 任意 probe-rs 支持的调试器，如 DAPLink / CMSIS-DAP、ST-Link、J-Link |
| 调试协议 | SWD（当前版本固定使用 SWD，暂不支持 JTAG） |
| 固件格式 | 目前仅支持 **Intel HEX（`.hex`）** |

## 快速开始

### 1. 准备 probe-rs

程序本身不内置烧录逻辑，所有烧录动作都通过调用 `probe-rs` 命令行完成。任选一种方式安装：

```powershell
# 方式一：官方安装脚本
powershell -ExecutionPolicy Bypass -c "irm https://github.com/probe-rs/probe-rs/releases/download/v0.32.0/probe-rs-tools-installer.ps1 | iex"

# 方式二：已有 Rust 工具链
cargo install probe-rs-tools --locked
```

也可以直接从 [probe-rs Releases](https://github.com/probe-rs/probe-rs/releases) 下载 `probe-rs-tools-x86_64-pc-windows-msvc.zip`，把其中的 `probe-rs.exe` 放到本程序的 `tools\` 目录下。

程序按以下顺序查找 `probe-rs.exe`，找到即用：

1. `App.config` 里的 `appSettings/ProbeRsPath`
2. 环境变量 `PROBE_RS_PATH`
3. 程序目录下的 `probe-rs.exe` 或 `tools\probe-rs.exe`
4. 程序所在目录的各级上级目录中的 `tools\probe-rs.exe`
5. 当前工作目录及其上级目录中的 `tools\probe-rs.exe`
6. `PATH` 环境变量中的各目录
7. `%USERPROFILE%\.cargo\bin\probe-rs.exe`

### 2. 准备芯片描述（yaml）

`yaml/` 目录存放 probe-rs 的 target 描述文件，每个文件对应一个芯片系列，文件名会直接显示在「芯片系列」下拉框里。

仓库已包含：

```
yaml/
├── GD32F30x_Series.yaml
├── GD32F3x0_Series.yaml
├── GD32F4xx_Series.yaml
├── STM32F0_Series.yaml
├── STM32F1_Series.yaml
├── STM32F3_Series.yaml
└── STM32F4_Series.yaml
```

要支持其他芯片，用 probe-rs 自带的 `target-gen` 从 ARM CMSIS pack 生成（只需在开发机做一次，程序运行时不联网）：

```powershell
# 安装 target-gen
cargo install target-gen --locked

# 从 pack 生成（OUTPUT 是目录，生成的 yaml 会直接放进去）
target-gen pack Keil.STM32F4xx_DFP.2.17.1.pack yaml
```

> pack 文件（`*.pack`）仅用于生成 yaml，体积较大且受各自的再分发许可约束，已加入 `.gitignore`，不会提交到仓库。

> 注意：`yaml` 目录会在「程序目录」及其上级目录中被自动搜索，所以直接在 `bin\Debug` 下运行、或把整个程序拷到别处都能正常读取。

### 3. 启动程序

双击 `DAP-Flash.exe`。程序启动后会自动加载芯片列表并扫描一次调试器。

## 使用步骤

1. **刷新调试器**：连接调试器后点「刷新」，在左侧下拉框中选择目标调试器（只连接一个时会自动选中）。
2. **选择芯片**：「芯片系列」选对应的 yaml 文件，「芯片型号」选具体型号（如 `STM32F401CCUx`）。**型号必须与实际芯片一致**，它决定了 Flash 算法和容量。
3. **设置速度与擦除方式**：一般保持 `1000` 与「自动」即可。
4. **打开固件**：点「打开文件」选择 `.hex`（也可以直接把文件拖到 HEX 视图上），右侧会显示解析后的内容，日志里会给出字节数与地址范围。
5. **开始烧录**：
   - 点「一键烧录」：连接芯片 → 擦除 → 写入 → 校验 → 复位，全自动。
   - 或者用四个分步按钮逐个执行。

### 各操作对应的 probe-rs 命令

| 界面操作 | 实际执行的命令 |
| --- | --- |
| 刷新 | `probe-rs list` |
| 连接芯片 | `probe-rs read b32 0xE000ED00 1`（读取内核 CPUID，只读） |
| 擦除Flash | `probe-rs erase`（整片擦除） |
| 写入 | `probe-rs download --binary-format hex <file>` |
| 校验 | `probe-rs verify --binary-format hex <file>` |
| 复位 | `probe-rs reset` |

所有命令都会带上 `--chip <型号> --chip-description-path <yaml> --protocol swd --speed <kHz> --probe <调试器> --non-interactive`，日志窗口里能看到完整命令行。

### 三种擦除方式

| 方式 | 行为 |
| --- | --- |
| 自动（默认） | 不做额外擦除，由 probe-rs 根据固件覆盖范围决定擦除策略 |
| 全部 | 写入前先执行 `probe-rs erase` 整片擦除 |
| 扇区 | 只擦除固件覆盖的扇区，并尽量保留同一扇区内未被固件覆盖的内容（probe-rs 支持 `--restore-unwritten` 时启用） |

> 「擦除Flash」按钮执行的是 **整片擦除**（probe-rs 的 `erase` 只有这一种粒度）。若当前选的是「扇区」，按钮仍会整片擦除并在日志中提示；扇区级擦除只发生在「一键烧录 / 写入」过程中。

## 常见问题

**启动后日志提示「未找到 probe-rs.exe」**

按上文「准备 probe-rs」安装，或把 `probe-rs.exe` 放到程序的 `tools\` 目录。先用命令行确认可用：

```powershell
probe-rs --version
probe-rs list
```

**刷新后提示「没有找到调试器」**

- 检查 USB 线与调试器指示灯；
- 确认没有其他程序（Keil、OpenOCD、pyOCD、另一个 probe-rs 进程）占用调试器——探针同一时间只能被一个进程打开；
- Linux 上需要 udev 规则，本工具面向 Windows。

**连接芯片失败 / `The connected chip could not automatically be determined`**

本工具不依赖 probe-rs 的自动识别，而是用你选的型号显式指定芯片。出现该类错误通常是：

- 芯片型号选错（换到与实际丝印一致的型号）；
- 芯片没上电、复位脚被拉低、SWD 线序接反；
- 芯片被读保护（RDP）锁定，需要先解除保护。

**校验失败：芯片中的内容与文件不一致**

- 确认写入与校验用的是同一个 `.hex`；
- 检查芯片是否有写保护区域；
- 换较低速度（如 1000 kHz 或 500 kHz）重试。

**日志里出现 `Unable to use specified speed ...`**

说明调试器不支持所选速率，probe-rs 已自动降速，功能不受影响。想避免该提示就选一个探针支持的速度。

**支持 .bin / .elf 吗？**

目前只支持 Intel HEX（`.hex`）。`.bin` 需要额外的基地址，`.elf` 的解析方式不同，暂未支持。

**HEX 视图里地址为什么从 `0x08000000` 开始？**

视图展示的是解析后的固件二进制，地址列即真实的 Flash 地址。若文件的数据跨度过大（超过 4 MB），为节省内存只显示第一段，具体分段信息会打印在日志里。

## 从源码构建

需要 Visual Studio 2022（含 .NET 桌面开发工作负载）或 MSBuild。

```powershell
# 还原 NuGet 包（首次构建）
nuget restore packages.config -PackagesDirectory packages

# 构建（Debug / Release）
dotnet build DAP-Flash.csproj -c Release
```

产物位于 `bin\Release\DAP-Flash.exe`，直接拷走即可运行（依赖 .NET Framework 4.8，无需安装）。

也可以用 Visual Studio 2022 打开 `DAP-Flash.slnx`（版本较旧的 VS 可直接打开 `DAP-Flash.csproj`）。

### 项目结构

```
DAP-Flash.csproj      工程文件（.NET Framework 4.8 / WinForms）
Form1.cs              界面逻辑：刷新、选择联动、烧录流程、日志
Form1.Designer.cs     界面布局
IntelHexFile.cs       Intel HEX 解析（分段、校验和、地址范围）
McuLibrary.cs         扫描 yaml 目录，提供芯片系列与型号列表
ProbeRsRunner.cs      定位并调用 probe-rs，解析输出与调试器列表
Program.cs            程序入口
yaml/                 芯片 target 描述文件
DFP/                  可选：ARM CMSIS pack，用于生成 yaml（*.pack 已在 .gitignore 中忽略）
tools/                可选：内置的 probe-rs.exe
```

## 工作原理

程序本身不实现 SWD 协议，而是作为 probe-rs 的图形前端：

1. 用 `probe-rs list` 枚举调试器，解析出 `VID:PID[:序列号]` 用于 `--probe` 精确定位；
2. 用 `--chip-description-path <yaml>` 把本地芯片描述加载进 probe-rs 的注册表，再用 `--chip <型号>` 精确匹配——因此**不需要联网、也不依赖 probe-rs 内置的芯片库**；
3. 依次调用 `erase` / `download` / `verify` / `reset` 完成烧录，并把子进程的实时输出转发到日志窗口。

## 致谢

- [probe-rs](https://github.com/probe-rs/probe-rs) —— 烧录后端（MIT / Apache-2.0，见 `tools\LICENSE-MIT`、`tools\LICENSE-APACHE`）
- [Be.HexEditor](https://github.com/jariq/Be.HexEditor) —— 十六进制显示控件 `Be.Windows.Forms.HexBox`（MIT，见 `bin\Release\LICENSE-HexBox.txt`）
- 芯片描述文件由 [target-gen](https://github.com/probe-rs/probe-rs) 从 ARM CMSIS Pack 生成（MIT / Apache-2.0）
- 全部第三方许可汇总见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)

## 许可证

[MIT](LICENSE)

## 支持

如果你觉得这个工具对你有帮助，欢迎扫码请我喝杯咖啡。打赏完全自愿，不影响任何功能的使用，也不提供额外权益。

![自愿打赏](assets/donate.jpg)
