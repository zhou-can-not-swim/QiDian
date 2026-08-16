using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using QiDian.Contracts.Config;
using QiDian.Contracts.Helpers;

namespace QiDian.Services;

/// <summary>
/// 全局热键服务：启动时根据设置注册「切换窗口」和「隐藏窗口」两个热键，
/// 收到热键消息后调用 WindowSwitcherService 完成窗口切换/隐藏。
/// </summary>
public interface IGlobalHotkeyService
{
    void Initialize(Window ownerWindow);
    void UnregisterAll();
}

public class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int HIDE_HOTKEY_ID = 8000;
    private const int SWITCH_HOTKEY_ID = 8001;
    private const int PLUGIN_HOTKEY_ID = 8002;

    private IntPtr _hwnd;
    private HwndSource? _source;
    private readonly IWindowSwitcherService _windowSwitcher;

    public GlobalHotkeyService(IWindowSwitcherService windowSwitcher)
    {
        _windowSwitcher = windowSwitcher;
    }

    public void Initialize(Window ownerWindow)
    {
        // 用 Loaded 优先级延迟注册，确保窗口句柄有效
        ownerWindow.Dispatcher.BeginInvoke(new Action(() =>
        {
            _hwnd = new WindowInteropHelper(ownerWindow).Handle;
            if (_hwnd == IntPtr.Zero)
                return;

            _source = HwndSource.FromHwnd(_hwnd);
            if (_source == null)
                return;

            _source.AddHook(HwndHook);

            var cfg = HotkeySettingsStore.Load();

            // 隐藏热键：未设置（modifiers/key 为 0）则跳过注册
            if (cfg.HideModifiers != 0 && cfg.HideKey != 0)
            {
                Register(HIDE_HOTKEY_ID, cfg.HideModifiers, cfg.HideKey, "隐藏热键");
            }

            // 切换热键
            if (cfg.SwitchModifiers != 0 && cfg.SwitchKey != 0)
            {
                Register(SWITCH_HOTKEY_ID, cfg.SwitchModifiers, cfg.SwitchKey, "切换热键");
            }

            // 插件窗口热键
            if (cfg.PluginModifiers != 0 && cfg.PluginKey != 0)
            {
                Register(PLUGIN_HOTKEY_ID, cfg.PluginModifiers, cfg.PluginKey, "插件热键");
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void Register(int id, int modifiers, int key, string name)
    {
        if (!HotKeyNativeMethods.RegisterHotKey(_hwnd, id, modifiers, key))
        {
            int err = Marshal.GetLastWin32Error();
            MessageBox.Show(
                $"「{name}」注册失败！错误码 {err}\n" +
                "常见原因：快捷键已被其它程序占用。",
                "全局热键");
        }
    }

    public void UnregisterAll()
    {
        if (_hwnd != IntPtr.Zero)
        {
            HotKeyNativeMethods.UnregisterHotKey(_hwnd, HIDE_HOTKEY_ID);
            HotKeyNativeMethods.UnregisterHotKey(_hwnd, SWITCH_HOTKEY_ID);
            HotKeyNativeMethods.UnregisterHotKey(_hwnd, PLUGIN_HOTKEY_ID);
        }
        _source?.RemoveHook(HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == HotKeyNativeMethods.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();

            if (hotkeyId == HIDE_HOTKEY_ID)
            {
                // 隐藏热键：隐藏所有窗口 / 再次触发显示主窗口
                _windowSwitcher.HideOrShowMainWindow();
                handled = true;
            }
            else if (hotkeyId == SWITCH_HOTKEY_ID)
            {
                // 切换热键：在 SearchWindow 与 NavWindow 之间切换
                _windowSwitcher.ToggleWindows();
                handled = true;
            }
            else if (hotkeyId == PLUGIN_HOTKEY_ID)
            {
                // 插件热键：在 SearchWindow 与 PluginWindow 之间切换
                _windowSwitcher.TogglePluginWindow();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }
}
