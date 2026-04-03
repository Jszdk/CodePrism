using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ILSpyGUI.Models;

/// <summary>
/// DLL文件信息模型
/// </summary>
public partial class DllFileInfo : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _status = "待处理";

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 所在文件夹
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件大小（格式化显示）
    /// </summary>
    public string FileSizeText => FormatFileSize(FileSize);

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; }

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
