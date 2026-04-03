using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyGUI.Services;

/// <summary>
/// 依赖管理器 - 自动检测和下载安装依赖
/// </summary>
public class DependencyManager
{
    private readonly HttpClient _httpClient;
    private readonly string _toolsDirectory;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<int>? ProgressChanged;

    public DependencyManager()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10);

        // 工具存放目录：用户主目录/.codeprism/tools
        _toolsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codeprism", "tools");

        Directory.CreateDirectory(_toolsDirectory);
    }

    /// <summary>
    /// 检查 ilspycmd 是否可用
    /// </summary>
    public (bool isInstalled, string? path) CheckILSpyCmd()
    {
        // 1. 检查内置路径
        var builtInPath = GetBuiltInILSpyCmdPath();
        if (File.Exists(builtInPath))
            return (true, builtInPath);

        // 2. 检查系统 PATH
        var systemPath = FindInPath("ilspycmd");
        if (!string.IsNullOrEmpty(systemPath))
            return (true, systemPath);

        return (false, null);
    }

    /// <summary>
    /// 检查 jadx 是否可用
    /// </summary>
    public (bool isInstalled, string? path) CheckJadx()
    {
        // 1. 检查内置路径
        var builtInPath = GetBuiltInJadxPath();
        if (File.Exists(builtInPath))
            return (true, builtInPath);

        // 2. 检查系统 PATH
        var systemPath = FindInPath("jadx");
        if (!string.IsNullOrEmpty(systemPath))
            return (true, systemPath);

        return (false, null);
    }

    /// <summary>
    /// 下载并安装 ilspycmd
    /// </summary>
    public async Task<bool> InstallILSpyCmdAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        try
        {
            StatusChanged?.Invoke(this, "正在下载 ilspycmd...");
            progress?.Report("下载 ilspycmd...");

            // ilspycmd 通过 dotnet tool 安装
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool install ilspycmd --global",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            await process.WaitForExitAsync(ct);

            progress?.Report("ilspycmd 安装完成");
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"安装失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 下载并安装 jadx
    /// </summary>
    public async Task<bool> InstallJadxAsync(IProgress<(string status, int progress)>? progress = null, CancellationToken ct = default)
    {
        try
        {
            var jadxDir = Path.Combine(_toolsDirectory, "jadx");
            Directory.CreateDirectory(jadxDir);

            StatusChanged?.Invoke(this, "正在下载 jadx...");
            progress?.Report(("下载 jadx...", 10));

            // jadx 下载地址
            var version = "1.4.7";
            var downloadUrl = GetJadxDownloadUrl(version);
            var zipPath = Path.Combine(jadxDir, $"jadx-{version}.zip");

            // 下载文件
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);

                var buffer = new byte[8192];
                long downloadedBytes = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var percent = (int)((downloadedBytes * 80) / totalBytes);
                        progress?.Report(("下载中...", percent));
                    }
                }
            }

            progress?.Report(("解压中...", 85));
            StatusChanged?.Invoke(this, "正在解压 jadx...");

            // 解压
            ZipFile.ExtractToDirectory(zipPath, jadxDir, true);

            // 删除 zip
            File.Delete(zipPath);

            progress?.Report(("jadx 安装完成", 100));
            StatusChanged?.Invoke(this, "jadx 安装完成");

            return true;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"安装失败: {ex.Message}");
            return false;
        }
    }

    private string GetBuiltInILSpyCmdPath()
    {
        // 检查用户目录下的 dotnet tools
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(homeDir, ".dotnet", "tools", "ilspycmd.exe");
        }
        else
        {
            return Path.Combine(homeDir, ".dotnet", "tools", "ilspycmd");
        }
    }

    private string GetBuiltInJadxPath()
    {
        var jadxDir = Path.Combine(_toolsDirectory, "jadx", "bin");

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(jadxDir, "jadx.bat");
        }
        else
        {
            return Path.Combine(jadxDir, "jadx");
        }
    }

    private string? FindInPath(string command)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var paths = pathEnv.Split(Path.PathSeparator);

        foreach (var path in paths)
        {
            string fullPath;
            if (OperatingSystem.IsWindows())
            {
                fullPath = Path.Combine(path, command + ".exe");
            }
            else
            {
                fullPath = Path.Combine(path, command);
            }

            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    private string GetJadxDownloadUrl(string version)
    {
        var baseUrl = $"https://github.com/skylot/jadx/releases/download/v{version}/jadx-{version}";

        if (OperatingSystem.IsWindows())
        {
            return baseUrl + ".zip";
        }
        else if (OperatingSystem.IsMacOS())
        {
            // macOS 也使用 zip 或考虑 homebrew
            return baseUrl + ".zip";
        }
        else
        {
            return baseUrl + ".zip";
        }
    }

    /// <summary>
    /// 获取工具路径（用于执行）
    /// </summary>
    public string? GetToolPath(string toolName)
    {
        return toolName.ToLower() switch
        {
            "ilspycmd" => CheckILSpyCmd().path,
            "jadx" => CheckJadx().path,
            _ => null
        };
    }
}
