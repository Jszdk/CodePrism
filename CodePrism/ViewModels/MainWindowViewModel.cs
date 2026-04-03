using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ILSpyGUI.Models;
using ILSpyGUI.Services;

namespace ILSpyGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DllScannerService _dllScanner;
    private readonly JavaScannerService _javaScanner;
    private readonly DecompilationService _decompilationService;
    private readonly JavaDecompilationService _javaDecompilationService;
    private CancellationTokenSource? _decompileCts;

    // 当前模式: 0=.NET, 1=Java
    [ObservableProperty]
    private int _selectedMode = 0;

    // .NET 相关
    [ObservableProperty]
    private ObservableCollection<DllFileInfo> _dllFiles = new();

    [ObservableProperty]
    private bool _ilSpyCmdInstalled;

    [ObservableProperty]
    private string _ilSpyCmdPath = string.Empty;

    // Java 相关
    [ObservableProperty]
    private ObservableCollection<JavaFileInfo> _javaFiles = new();

    [ObservableProperty]
    private bool _jadxInstalled;

    [ObservableProperty]
    private string _jadxPath = string.Empty;

    // 通用
    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "选择 .NET 或 Java 标签开始";

    [ObservableProperty]
    private bool _isDecompiling;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private int _progressMaximum = 100;

    public MainWindowViewModel()
    {
        _dllScanner = new DllScannerService();
        _javaScanner = new JavaScannerService();
        _decompilationService = new DecompilationService();
        _javaDecompilationService = new JavaDecompilationService();
        _ = CheckToolsAsync();
    }

    private async Task CheckToolsAsync()
    {
        await CheckILSpyCmdAsync();
        await CheckJadxAsync();
    }

    private async Task CheckILSpyCmdAsync()
    {
        try
        {
            var (isInstalled, path, version) = await _decompilationService.CheckILSpyCmdInstallationAsync();
            IlSpyCmdInstalled = isInstalled;

            if (isInstalled)
            {
                IlSpyCmdPath = path ?? "ilspycmd";
                AppendLog($"[.NET] ✓ ilspycmd 已就绪");
            }
            else
            {
                AppendLog($"[.NET] ✗ ilspycmd 未找到 - dotnet tool install --global ilspycmd");
            }
        }
        catch (Exception ex)
        {
            IlSpyCmdInstalled = false;
            AppendLog($"[.NET] ✗ ilspycmd 检测失败: {ex.Message}");
        }
    }

    private async Task CheckJadxAsync()
    {
        try
        {
            var (isInstalled, path, version) = await _javaDecompilationService.CheckJadxInstallationAsync();
            JadxInstalled = isInstalled;

            if (isInstalled)
            {
                JadxPath = path ?? "jadx";
                AppendLog($"[Java] ✓ jadx 已就绪");
            }
            else
            {
                AppendLog($"[Java] ✗ jadx 未找到 - https://github.com/skylot/jadx");
            }
        }
        catch (Exception ex)
        {
            JadxInstalled = false;
            AppendLog($"[Java] ✗ jadx 检测失败: {ex.Message}");
        }
    }

    // .NET 命令
    [RelayCommand]
    private async Task SelectNetDirectory()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "扫描 .NET 项目目录",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        IsScanning = true;
        StatusMessage = "扫描 .NET DLL/EXE...";

        await Task.Run(() =>
        {
            var files = _dllScanner.ScanDirectory(folders[0].Path.LocalPath);
            foreach (var file in files)
            {
                if (!DllFiles.Any(f => f.FilePath == file.FilePath))
                {
                    _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => DllFiles.Add(file));
                }
            }
            AppendLog($"[.NET] 找到 {files.Count} 个文件，列表共 {DllFiles.Count} 个");
            StatusMessage = $"已添加 {files.Count} 个 .NET 文件";
        });

        IsScanning = false;
    }

    [RelayCommand]
    private async Task SelectNetFiles()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 DLL/EXE 文件",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(".NET Assembly") { Patterns = new[] { "*.dll", "*.exe" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (!DllFiles.Any(f => f.FilePath == path))
            {
                var info = new FileInfo(path);
                DllFiles.Add(new DllFileInfo
                {
                    FilePath = path,
                    FileName = info.Name,
                    Directory = info.DirectoryName ?? string.Empty,
                    FileSize = info.Length,
                    LastModified = info.LastWriteTime,
                    IsSelected = true
                });
            }
        }
        StatusMessage = $"已添加 {files.Count} 个 .NET 文件";
    }

    // Java 命令
    [RelayCommand]
    private async Task SelectJavaDirectory()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "扫描 Java 项目目录",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        IsScanning = true;
        StatusMessage = "扫描 Java 文件...";

        await Task.Run(() =>
        {
            var files = _javaScanner.ScanDirectory(folders[0].Path.LocalPath);
            foreach (var file in files)
            {
                if (!JavaFiles.Any(f => f.FilePath == file.FilePath))
                {
                    _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => JavaFiles.Add(file));
                }
            }
            AppendLog($"[Java] 找到 {files.Count} 个文件，列表共 {JavaFiles.Count} 个");
            StatusMessage = $"已添加 {files.Count} 个 Java 文件";
        });

        IsScanning = false;
    }

    [RelayCommand]
    private async Task SelectJavaFiles()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 JAR/APK/CLASS 文件",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Java Files") { Patterns = new[] { "*.jar", "*.war", "*.apk", "*.dex", "*.class" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (!JavaFiles.Any(f => f.FilePath == path))
            {
                var info = new FileInfo(path);
                JavaFiles.Add(new JavaFileInfo
                {
                    FilePath = path,
                    FileName = info.Name,
                    Directory = info.DirectoryName ?? string.Empty,
                    FileSize = info.Length,
                    LastModified = info.LastWriteTime,
                    IsSelected = true
                });
            }
        }
        StatusMessage = $"已添加 {files.Count} 个 Java 文件";
    }

    // 通用命令
    [RelayCommand]
    private async Task SelectOutputDirectory()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择反编译输出目录"
        });

        if (folders.Count > 0)
        {
            OutputDirectory = folders[0].Path.LocalPath;
            StatusMessage = $"输出目录: {OutputDirectory}";
        }
    }

    [RelayCommand]
    private void ClearNetFiles() => DllFiles.Clear();

    [RelayCommand]
    private void ClearJavaFiles() => JavaFiles.Clear();

    [RelayCommand]
    private void SelectAllNet() { foreach (var f in DllFiles) f.IsSelected = true; }

    [RelayCommand]
    private void DeselectAllNet() { foreach (var f in DllFiles) f.IsSelected = false; }

    [RelayCommand]
    private void SelectAllJava() { foreach (var f in JavaFiles) f.IsSelected = true; }

    [RelayCommand]
    private void DeselectAllJava() { foreach (var f in JavaFiles) f.IsSelected = false; }

    [RelayCommand]
    private async Task StartDecompile()
    {
        if (SelectedMode == 0)
            await StartNetDecompile();
        else
            await StartJavaDecompile();
    }

    private async Task StartNetDecompile()
    {
        if (!IlSpyCmdInstalled)
        {
            AppendLog("错误: ilspycmd 未安装");
            return;
        }

        var selectedFiles = DllFiles.Where(f => f.IsSelected).ToList();
        if (selectedFiles.Count == 0)
        {
            AppendLog("错误: 请选择 .NET 文件");
            return;
        }

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            AppendLog("错误: 请选择输出目录");
            return;
        }

        IsDecompiling = true;
        _decompileCts = new CancellationTokenSource();
        ProgressValue = 0;
        ProgressMaximum = selectedFiles.Count;

        AppendLog($"\n[.NET] 开始反编译 {selectedFiles.Count} 个文件...");

        var progress = new Progress<(int current, int total, string message)>(p =>
        {
            ProgressValue = p.current;
            StatusMessage = p.message;
        });

        try
        {
            var result = await _decompilationService.DecompileBatchAsync(
                selectedFiles, OutputDirectory, progress, _decompileCts.Token);

            AppendLog($"[.NET] 完成! 成功: {result.SuccessCount}, 失败: {result.FailedCount}");
        }
        catch (Exception ex)
        {
            AppendLog($"错误: {ex.Message}");
        }
        finally
        {
            IsDecompiling = false;
        }
    }

    private async Task StartJavaDecompile()
    {
        if (!JadxInstalled)
        {
            AppendLog("错误: jadx 未安装");
            return;
        }

        var selectedFiles = JavaFiles.Where(f => f.IsSelected).ToList();
        if (selectedFiles.Count == 0)
        {
            AppendLog("错误: 请选择 Java 文件");
            return;
        }

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            AppendLog("错误: 请选择输出目录");
            return;
        }

        IsDecompiling = true;
        _decompileCts = new CancellationTokenSource();
        ProgressValue = 0;
        ProgressMaximum = selectedFiles.Count;

        AppendLog($"\n[Java] 开始反编译 {selectedFiles.Count} 个文件...");

        var progress = new Progress<(int current, int total, string message)>(p =>
        {
            ProgressValue = p.current;
            StatusMessage = p.message;
        });

        try
        {
            var result = await _javaDecompilationService.DecompileBatchAsync(
                selectedFiles, OutputDirectory, progress, _decompileCts.Token);

            AppendLog($"[Java] 完成! 成功: {result.SuccessCount}, 失败: {result.FailedCount}");
        }
        catch (Exception ex)
        {
            AppendLog($"错误: {ex.Message}");
        }
        finally
        {
            IsDecompiling = false;
        }
    }

    [RelayCommand]
    private void CancelDecompile()
    {
        _decompileCts?.Cancel();
        StatusMessage = "正在取消...";
    }

    private void AppendLog(string message)
    {
        LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }

    private static TopLevel? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return lifetime.MainWindow;
        }
        return null;
    }
}
