using CommunityToolkit.Mvvm.ComponentModel;

namespace ILSpyGUI.Models;

/// <summary>
/// 反编译选项（简化版 - 仅用于代码审计）
/// </summary>
public partial class DecompileOptions : ObservableObject
{
    [ObservableProperty]
    private string _outputDirectory = string.Empty;
}
