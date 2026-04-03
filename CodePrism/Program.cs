using Avalonia;
using System;
using System.IO;
using System.Linq;

namespace ILSpyGUI;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 自动检测并设置 DOTNET_ROOT（解决 ilspycmd 找不到 runtime 的问题）
        EnsureDotNetRoot();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void EnsureDotNetRoot()
    {
        // 如果已经设置了，直接返回
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT_ARM64")))
        {
            return;
        }

        // 尝试找到 .NET Runtime 安装路径
        var possiblePaths = new[]
        {
            // Homebrew .NET 8
            "/opt/homebrew/Cellar/dotnet@8/8.0.125/libexec",
            "/opt/homebrew/Cellar/dotnet/8.0.125/libexec",
            // Homebrew .NET (当前版本)
            "/opt/homebrew/Cellar/dotnet/current/libexec",
            // 标准安装路径
            "/usr/local/share/dotnet",
            "/usr/share/dotnet",
            // 用户主目录
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet")
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path) &&
                (Directory.Exists(Path.Combine(path, "shared", "Microsoft.NETCore.App")) ||
                 File.Exists(Path.Combine(path, "dotnet"))))
            {
                Environment.SetEnvironmentVariable("DOTNET_ROOT", path);
                Console.WriteLine($"[INFO] 自动设置 DOTNET_ROOT={path}");
                break;
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
