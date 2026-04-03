using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ILSpyGUI.Models;

namespace ILSpyGUI.Services;

/// <summary>
/// 反编译服务（简化版）
/// </summary>
public class DecompilationService
{
    private string? _ilSpyCmdPath;

    /// <summary>
    /// 获取ilspycmd可执行文件路径
    /// </summary>
    private async Task<string?> FindILSpyCmdPathAsync()
    {
        if (_ilSpyCmdPath != null)
            return _ilSpyCmdPath;

        // 尝试的路径列表
        var pathsToTry = new List<string> { "ilspycmd" };

        // 用户目录下的 dotnet tools 路径
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        pathsToTry.Add(Path.Combine(homeDir, ".dotnet", "tools", "ilspycmd"));
        pathsToTry.Add(Path.Combine(homeDir, ".dotnet", "tools", "ilspycmd.exe"));

        // Windows 路径
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            pathsToTry.Add(Path.Combine(localAppData, "Microsoft", "dotnet", "tools", "ilspycmd.exe"));
        }

        foreach (var path in pathsToTry)
        {
            try
            {
                var result = await ExecuteCommandAsync(path, "--version");
                if (result.exitCode == 0)
                {
                    _ilSpyCmdPath = path;
                    return path;
                }
            }
            catch
            {
                // 继续尝试下一个路径
            }
        }

        return null;
    }

    /// <summary>
    /// 检查ilspycmd是否已安装
    /// </summary>
    public async Task<(bool isInstalled, string? path, string? version)> CheckILSpyCmdInstallationAsync()
    {
        var path = await FindILSpyCmdPathAsync();
        if (path == null)
        {
            return (false, null, null);
        }

        try
        {
            var result = await ExecuteCommandAsync(path, "--version");
            return (result.exitCode == 0, path, result.output.Trim());
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// 反编译单个DLL文件
    /// </summary>
    public async Task<(bool success, string? error)> DecompileAsync(
        DllFileInfo dllFile,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var ilSpyPath = await FindILSpyCmdPathAsync();
        if (ilSpyPath == null)
        {
            return (false, "ilspycmd 未找到");
        }

        // 构建输出路径：在输出目录下创建与DLL同名的子目录
        var dllOutputDir = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(dllFile.FileName));
        Directory.CreateDirectory(dllOutputDir);

        // 构建参数：只反编译源码，不带项目文件
        var args = $"-o \"{dllOutputDir}\" \"{dllFile.FilePath}\"";

        var result = await ExecuteCommandAsync(ilSpyPath, args, cancellationToken);

        return result.exitCode == 0
            ? (true, null)
            : (false, result.error);
    }

    /// <summary>
    /// 批量反编译
    /// </summary>
    public async Task<DecompilationResult> DecompileBatchAsync(
        List<DllFileInfo> dllFiles,
        string outputDirectory,
        IProgress<(int current, int total, string message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DecompilationResult();

        for (int i = 0; i < dllFiles.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.WasCancelled = true;
                break;
            }

            var file = dllFiles[i];
            file.Status = "反编译中...";

            progress?.Report((i + 1, dllFiles.Count, $"正在反编译: {file.FileName}"));

            var (success, error) = await DecompileAsync(file, outputDirectory, cancellationToken);

            if (success)
            {
                file.Status = "完成";
                result.SuccessCount++;
            }
            else
            {
                file.Status = "失败";
                result.Errors.Add((file.FileName, error ?? "未知错误"));
            }
        }

        return result;
    }

    private async Task<(int exitCode, string output, string error)> ExecuteCommandAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }
}

/// <summary>
/// 反编译结果
/// </summary>
public class DecompilationResult
{
    public int SuccessCount { get; set; }
    public int FailedCount => Errors.Count;
    public bool WasCancelled { get; set; }
    public List<(string FileName, string Error)> Errors { get; set; } = new();
}
