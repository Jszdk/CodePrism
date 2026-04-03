using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ILSpyGUI.Models;

/// <summary>
/// Java 文件信息模型
/// </summary>
public partial class JavaFileInfo : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _status = "待处理";

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public string FileSizeText => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{bytes / (double)GB:F2} GB";
        if (bytes >= MB)
            return $"{bytes / (double)MB:F2} MB";
        if (bytes >= KB)
            return $"{bytes / (double)KB:F2} KB";
        return $"{bytes} B";
    }
}
