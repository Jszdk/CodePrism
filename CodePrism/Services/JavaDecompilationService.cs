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
/// Java 反编译服务 (使用 jadx)
/// </summary>
public class JavaDecompilationService
{
    private string? _jadxPath;

    /// <summary>
    /// 查找 jadx 路径
    /// </summary>
    private async Task<string?> FindJadxPathAsync()
    {
        if (_jadxPath != null)
            return _jadxPath;

        var pathsToTry = new List<string> { "jadx" };

        // 常见安装路径
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        pathsToTry.Add(Path.Combine(homeDir, "jadx", "bin", "jadx"));
        pathsToTry.Add(Path.Combine(homeDir, "jadx", "bin", "jadx.bat"));
        pathsToTry.Add(Path.Combine("/usr", "local", "bin", "jadx"));
        pathsToTry.Add(Path.Combine("/opt", "homebrew", "bin", "jadx"));

        if (OperatingSystem.IsWindows())
        {
            pathsToTry.Add(Path.Combine("C:", "Program Files", "jadx", "bin", "jadx.bat"));
            pathsToTry.Add(Path.Combine("C:", "Program Files (x86)", "jadx", "bin", "jadx.bat"));
        }

        foreach (var path in pathsToTry)
        {
            try
            {
                var result = await ExecuteCommandAsync(path, "--version");
                if (result.exitCode == 0)
                {
                    _jadxPath = path;
                    return path;
                }
            }
            catch
            {
                // 继续尝试
            }
        }

        return null;
    }

    /// <summary>
    /// 检查 jadx 是否已安装
    /// </summary>
    public async Task<(bool isInstalled, string? path, string? version)> CheckJadxInstallationAsync()
    {
        var path = await FindJadxPathAsync();
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
    /// 反编译 Java 文件
    /// </summary>
    public async Task<(bool success, string? error)> DecompileAsync(
        JavaFileInfo javaFile,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var jadxPath = await FindJadxPathAsync();
        if (jadxPath == null)
        {
            return (false, "jadx 未找到");
        }

        // 构建输出路径
        var fileOutputDir = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(javaFile.FileName));
        Directory.CreateDirectory(fileOutputDir);

        // jadx 参数
        var args = $"-d \"{fileOutputDir}\" \"{javaFile.FilePath}\"";

        var result = await ExecuteCommandAsync(jadxPath, args, cancellationToken);

        return result.exitCode == 0
            ? (true, null)
            : (false, result.error);
    }

    /// <summary>
    /// 批量反编译
    /// </summary>
    public async Task<DecompilationResult> DecompileBatchAsync(
        List<JavaFileInfo> files,
        string outputDirectory,
        IProgress<(int current, int total, string message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DecompilationResult();

        for (int i = 0; i < files.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.WasCancelled = true;
                break;
            }

            var file = files[i];
            file.Status = "反编译中...";

            progress?.Report((i + 1, files.Count, $"正在反编译: {file.FileName}"));

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
