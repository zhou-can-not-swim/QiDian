using System.IO;
using System.Text.Json;

namespace QiDian.Contracts.Config;

/// <summary>
/// 全局热键配置。主程序和设置插件（QiDian.Nav.Settings）都通过
/// HotkeySettingsStore 读写，避免插件与主程序直接耦合。
/// </summary>
public sealed class HotkeyConfig
{
    /// <summary>隐藏热键的修饰键（MOD_* 组合值）</summary>
    public int HideModifiers { get; set; }

    /// <summary>隐藏热键的按键（虚拟键码）</summary>
    public int HideKey { get; set; }

    /// <summary>切换热键的修饰键（MOD_* 组合值）</summary>
    public int SwitchModifiers { get; set; }

    /// <summary>切换热键的按键（虚拟键码）</summary>
    public int SwitchKey { get; set; }

    /// <summary>插件窗口热键的修饰键（MOD_* 组合值）</summary>
    public int PluginModifiers { get; set; }

    /// <summary>插件窗口热键的按键（虚拟键码）</summary>
    public int PluginKey { get; set; }
}

public static class HotkeySettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QiDian",
        "hotkeys.json");

    /// <summary>读取热键配置，文件不存在或损坏时返回默认值（未设置）</summary>
    public static HotkeyConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var cfg = JsonSerializer.Deserialize<HotkeyConfig>(json);
                if (cfg != null)
                    return cfg;
            }
        }
        catch
        {
            // 配置损坏时回退到默认值，不阻断主流程
        }
        return new HotkeyConfig();
    }

    /// <summary>保存热键配置</summary>
    public static void Save(HotkeyConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // 写入失败不阻塞保存操作，用户仍可在界面看到反馈
        }
    }
}
