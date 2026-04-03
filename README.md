# 💎 CodePrism

> 代码棱镜 - 看透每一行源码

一款支持 **.NET** 和 **Java** 的双模式跨平台反编译工具，专为**代码审计**场景设计。

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-8-512BD4.svg)

---

## 🎯 为什么选择 CodePrism

在代码审计工作中，我们经常需要分析各种语言编译后的二进制文件：

- **.NET 项目** → 需要反编译 `DLL`/`EXE` 文件
- **Java 项目** → 需要反编译 `JAR`/`APK`/`CLASS` 文件

CodePrism 将这两种需求统一在一个简洁的界面中，让你无需切换工具，高效完成审计任务。

---

## ✨ 功能特性

| 特性 | 描述 |
|------|------|
| 🎯 **双模式支持** | 标签页切换 .NET 和 Java 反编译 |
| 📁 **批量扫描** | 递归扫描目录，自动发现可反编译文件 |
| 📂 **智能输出** | 每个文件输出到独立目录，源码结构清晰 |
| 🚀 **一键反编译** | 选择文件 → 设置输出 → 开始 |
| 🎨 **优雅界面** | 现代化 UI，支持 macOS/Windows/Linux |
| 📊 **实时进度** | 进度条 + 日志，反编译状态一目了然 |

---

## 📦 支持的文件格式

### .NET 模式
| 扩展名 | 说明 |
|--------|------|
| `.dll` | 动态链接库 |
| `.exe` | 可执行程序 |

### Java 模式
| 扩展名 | 说明 |
|--------|------|
| `.jar` | Java 归档 |
| `.war` | Web 应用归档 |
| `.apk` | Android 安装包 |
| `.dex` | Dalvik 可执行文件 |
| `.class` | Java 字节码 |

---

## 🛠️ 安装

### 1. 安装 .NET 8 SDK

```bash
# macOS (Homebrew)
brew install dotnet@8

# 其他平台
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. 安装反编译工具

#### ILSpy (.NET)
```bash
dotnet tool install --global ilspycmd
```

#### jadx (Java)
```bash
# macOS
brew install jadx

# 或手动安装
# https://github.com/skylot/jadx/releases
```

### 3. 运行 CodePrism

```bash
# 克隆仓库
git clone https://github.com/595614096/CodePrism.git
cd CodePrism

# 运行
dotnet run --project ILSpyGUI
```

---

## 📖 使用指南

### 基本流程

1. **选择模式**
   - 点击顶部 `.NET (ILSpy)` 或 `Java (jadx)` 标签

2. **添加文件**
   - `📁 扫描目录` - 递归扫描整个项目
   - `📄 添加文件` - 手动选择单个/多个文件

3. **设置输出**（可选）
   - 默认输出到桌面或上次选择的位置
   - 点击 `浏览...` 自定义输出目录

4. **开始反编译**
   - 勾选需要处理的文件
   - 点击 `🚀 开始反编译`

### 输出结构

```
输出目录/
├── Assembly1/              # .NET 项目
│   ├── Program.cs
│   ├── Services/
│   │   └── AuthService.cs
│   └── ...
├── MyApp/                  # Java/Android 项目
│   ├── MainActivity.java
│   ├── utils/
│   │   └── Crypto.java
│   └── ...
```

---

## 🖥️ 发布为独立应用

无需安装 .NET Runtime，直接运行：

```bash
# Windows x64
dotnet publish ILSpyGUI/ILSpyGUI.csproj \
  -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true

# macOS ARM64 (Apple Silicon)
dotnet publish ILSpyGUI/ILSpyGUI.csproj \
  -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true

# Linux x64
dotnet publish ILSpyGUI/ILSpyGUI.csproj \
  -c Release -r linux-x64 --self-contained \
  -p:PublishSingleFile=true
```

输出文件在 `ILSpyGUI/bin/Release/net8.0/<runtime>/publish/`

---

## 🐛 常见问题

### Q: 提示 "ilspycmd 未就绪"

确保已安装 ILSpy 命令行工具：
```bash
dotnet tool install --global ilspycmd
# 验证
ilspycmd --version
```

### Q: 提示 "jadx 未安装"

```bash
# macOS
brew install jadx

# 验证
jadx --version
```

### Q: 反编译后的代码可以编译吗？

- **.NET**: 通常可以，但混淆过的代码可能需要手动修复
- **Java**: jadx 会尽可能生成可编译的 Java 代码

### Q: macOS 提示 "无法打开应用"

在「系统设置 > 隐私与安全性」中允许应用运行。

---

## 🤝 贡献

欢迎提交 Issue 和 PR！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

---

## 📄 许可证

[MIT](LICENSE) © 2024 CodePrism

---

## 🙏 致谢

| 项目 | 用途 |
|------|------|
| [ICSharpCode/ILSpy](https://github.com/icsharpcode/ILSpy) | .NET 反编译 |
| [skylot/jadx](https://github.com/skylot/jadx) | Java/Android 反编译 |
| [Avalonia UI](https://avaloniaui.net/) | 跨平台 UI 框架 |

---

> 💡 **小贴士**: CodePrism 专为代码审计设计，如果你有复杂的反编译需求（如 .NET 混淆代码），可能需要配合其他专业工具使用。
