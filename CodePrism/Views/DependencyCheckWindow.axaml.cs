using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ILSpyGUI.Services;

namespace ILSpyGUI.Views;

public partial class DependencyCheckWindow : Window
{
    private readonly DependencyManager _dependencyManager;

    public bool DependenciesInstalled { get; private set; }

    public DependencyCheckWindow()
    {
        InitializeComponent();

        _dependencyManager = new DependencyManager();
        _dependencyManager.StatusChanged += (s, msg) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressStatus.Text = msg;
            });
        };

        Loaded += OnLoaded;
        AutoInstallButton.Click += OnAutoInstallClick;
        ManualButton.Click += OnManualClick;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        await CheckDependenciesAsync();
    }

    private async Task CheckDependenciesAsync()
    {
        var ilspy = _dependencyManager.CheckILSpyCmd();
        var jadx = _dependencyManager.CheckJadx();

        ILSpyStatus.Text = ilspy.isInstalled ? "✅ 已安装" : "❌ 未安装";
        ILSpyStatus.Foreground = ilspy.isInstalled ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981")) : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF4444"));

        JadxStatus.Text = jadx.isInstalled ? "✅ 已安装" : "❌ 未安装";
        JadxStatus.Foreground = jadx.isInstalled ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981")) : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF4444"));

        if (ilspy.isInstalled && jadx.isInstalled)
        {
            DependenciesInstalled = true;
            await Task.Delay(500);
            Close();
        }
    }

    private async void OnAutoInstallClick(object? sender, RoutedEventArgs e)
    {
        AutoInstallButton.IsEnabled = false;
        ManualButton.IsEnabled = false;
        ProgressPanel.IsVisible = true;
        InfoText.Text = "正在下载和安装依赖，请稍候...";

        var cts = new CancellationTokenSource();
        var progress = new Progress<(string status, int progress)>(p =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressStatus.Text = p.status;
                ProgressBar.Value = p.progress;
            });
        });

        // 安装 ilspycmd
        if (!_dependencyManager.CheckILSpyCmd().isInstalled)
        {
            ILSpyStatus.Text = "⬇️ 下载中...";
            var ilspyProgress = new Progress<string>(msg =>
            {
                Dispatcher.UIThread.InvokeAsync(() => ProgressStatus.Text = msg);
            });
            var success = await _dependencyManager.InstallILSpyCmdAsync(ilspyProgress, cts.Token);
            ILSpyStatus.Text = success ? "✅ 已安装" : "❌ 安装失败";
            ILSpyStatus.Foreground = success ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981")) : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF4444"));
        }

        // 安装 jadx
        if (!_dependencyManager.CheckJadx().isInstalled)
        {
            JadxStatus.Text = "⬇️ 下载中...";
            var jadxProgress = new Progress<(string status, int value)>(p =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressStatus.Text = p.status;
                    ProgressBar.Value = p.value;
                });
            });
            var success = await _dependencyManager.InstallJadxAsync(jadxProgress, cts.Token);
            JadxStatus.Text = success ? "✅ 已安装" : "❌ 安装失败";
            JadxStatus.Foreground = success ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10B981")) : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EF4444"));
        }

        // 重新检查
        await CheckDependenciesAsync();

        if (DependenciesInstalled)
        {
            InfoText.Text = "所有依赖已安装完成！即将启动 CodePrism...";
            await Task.Delay(1000);
            Close();
        }
        else
        {
            AutoInstallButton.IsEnabled = true;
            ManualButton.IsEnabled = true;
            InfoText.Text = "部分依赖安装失败，请重试或手动安装。";
        }
    }

    private void OnManualClick(object? sender, RoutedEventArgs e)
    {
        // 打开浏览器显示安装指南
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/Jszdk/CodePrism#安装",
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);

        Close();
    }
}
