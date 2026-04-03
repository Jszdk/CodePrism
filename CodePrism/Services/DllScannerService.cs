using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILSpyGUI.Models;

namespace ILSpyGUI.Services;

/// <summary>
/// DLL文件扫描服务
/// </summary>
public class DllScannerService
{
    /// <summary>
    /// 扫描目录中的所有DLL/EXE文件（递归）
    /// </summary>
    public List<DllFileInfo> ScanDirectory(string directory)
    {
        var results = new List<DllFileInfo>();

        if (!Directory.Exists(directory))
            return results;

        var extensions = new[] { ".dll", ".exe" };
        var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));

        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                results.Add(new DllFileInfo
                {
                    FilePath = file,
                    FileName = info.Name,
                    Directory = info.DirectoryName ?? string.Empty,
                    FileSize = info.Length,
                    LastModified = info.LastWriteTime,
                    IsSelected = true
                });
            }
            catch
            {
                // 忽略无法访问的文件
            }
        }

        return results;
    }
}
