using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;
using System.Linq;
using ILSpyGUI.Services;
using ILSpyGUI.Views;

namespace ILSpyGUI;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 自动检测并设置 DOTNET_ROOT
        EnsureDotNetRoot();

        // 检查依赖
        var depManager = new DependencyManager();
        var ilspy = depManager.CheckILSpyCmd();
        var jadx = depManager.CheckJadx();

        // 如果依赖未安装，显示安装对话框
        if (!ilspy.isInstalled || !jadx.isInstalled)
        {
            // 启动 Avalonia 显示对话框
            var builder = BuildAvaloniaApp();
            builder.SetupWithoutStarting();

            var checkWindow = new DependencyCheckWindow();
            checkWindow.Show();
            checkWindow.Closed += (s, e) =>
            {
                // 对话框关闭后继续启动主窗口
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            };
        }
        else
        {
            // 直接启动主程序
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    private static void EnsureDotNetRoot()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT_ARM64")))
        {
            return;
        }

        var possiblePaths = new[]
        {
            "/opt/homebrew/Cellar/dotnet@8/8.0.125/libexec",
            "/opt/homebrew/Cellar/dotnet/8.0.125/libexec",
            "/opt/homebrew/Cellar/dotnet/current/libexec",
            "/usr/local/share/dotnet",
            "/usr/share/dotnet",
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
