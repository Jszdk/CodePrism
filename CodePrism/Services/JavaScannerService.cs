using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILSpyGUI.Models;

namespace ILSpyGUI.Services;

/// <summary>
/// Java 文件扫描服务
/// </summary>
public class JavaScannerService
{
    /// <summary>
    /// 扫描目录中的 Java 相关文件
    /// </summary>
    public List<JavaFileInfo> ScanDirectory(string directory)
    {
        var results = new List<JavaFileInfo>();

        if (!Directory.Exists(directory))
            return results;

        // Java 相关扩展名
        var extensions = new[] { ".jar", ".war", ".ear", ".apk", ".dex", ".class" };

        var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));

        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                results.Add(new JavaFileInfo
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
