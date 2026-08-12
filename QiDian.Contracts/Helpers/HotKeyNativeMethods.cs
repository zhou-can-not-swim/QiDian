using System.Runtime.InteropServices;

namespace QiDian.Contracts.Helpers;

/// <summary>
/// 全局热键 P/Invoke 封装。主程序注册热键、设置插件解析热键组合时共用，
/// 保证两边的 MOD_* 常量一致。
/// </summary>
public static class HotKeyNativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int MOD_NONE = 0x0000;
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
