# 第三方组件致谢与许可声明（Third-Party Notices）

本仓库包含或引用了以下第三方组件，各自按对应许可使用，特此致谢并附许可说明。

## Be.Windows.Forms.HexBox — MIT License

- 用途：十六进制显示控件，随编译产物 `bin\` 分发（`Be.Windows.Forms.HexBox.dll`）。
- 来源：<https://github.com/jariq/Be.HexEditor>
- 许可文本：`bin\Debug\LICENSE-HexBox.txt`、`bin\Release\LICENSE-HexBox.txt`（与 DLL 同目录随行）

MIT 许可全文如下：

```
The MIT License

Copyright (c) 2011 Bernhard Elbl

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
```

## probe-rs — MIT OR Apache-2.0

- 用途：烧录后端，随仓库分发（`tools\probe-rs.exe`）。
- 来源：<https://github.com/probe-rs/probe-rs>
- 许可文本：`tools\LICENSE-MIT`、`tools\LICENSE-APACHE`

## 芯片描述文件（yaml\）— MIT OR Apache-2.0

- 用途：probe-rs 的 target 描述文件，本仓库 `yaml\` 目录下的文件由 probe-rs 的 `target-gen` 从 ARM CMSIS Pack 生成，归 probe-rs 项目所有。
- 许可文本：`tools\LICENSE-MIT`、`tools\LICENSE-APACHE`

## 说明：本仓库不随分发 ARM CMSIS Pack（\*.pack）

Keil / GigaDevice 的器件包受各自许可约束（其中 GigaDevice 的器件包为专有软件，厂商协议明确禁止再分发），已通过 `.gitignore` 排除，**不随仓库分发**。如需生成芯片描述，请自行从官方渠道获取对应 Pack，或用 `target-gen` 生成。
