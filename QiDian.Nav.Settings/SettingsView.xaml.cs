using QiDian.Contracts.Config;
using QiDian.Contracts.Helpers;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QiDian.Nav.Settings
{
    public partial class SettingsView : UserControl, IViewFor<SettingsViewModel>
    {
        private int _hModifiers = 0;
        private int _hKey = 0;

        private int _sModifiers = 0;
        private int _sKey = 0;

        private int _pModifiers = 0;
        private int _pKey = 0;

        public SettingsView()
        {
            InitializeComponent();

            this.KeyDown += SettingsView_KeyDown;
            LoadSavedHotkey();

            this.WhenActivated(disposables =>
            {
                // 绑定逻辑在此注册
            });
        }

        #region ViewModel

        public SettingsViewModel? ViewModel
        {
            get => (SettingsViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (SettingsViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SettingsViewModel), typeof(SettingsView),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is SettingsView view)
                        view.DataContext = e.NewValue;
                }));

        #endregion

        #region 加载已保存的热键

        private void LoadSavedHotkey()
        {
            var cfg = HotkeySettingsStore.Load();

            _hModifiers = cfg.HideModifiers;
            _hKey = cfg.HideKey;
            HHotkeyTextBox.Text = FormatHotkeyText(_hModifiers, _hKey);

            _sModifiers = cfg.SwitchModifiers;
            _sKey = cfg.SwitchKey;
            SHotkeyTextBox.Text = FormatHotkeyText(_sModifiers, _sKey);

            _pModifiers = cfg.PluginModifiers;
            _pKey = cfg.PluginKey;
            PHotkeyTextBox.Text = FormatHotkeyText(_pModifiers, _pKey);
        }

        #endregion

        #region 捕获热键

        private void SettingsView_KeyDown(object sender, KeyEventArgs e)
        {
            // Alt 组合键在 WPF 中 Key 会变成 Key.System，真实按键在 e.SystemKey
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // 忽略修饰键单独按下
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            var focusedElement = Keyboard.FocusedElement as TextBox;
            if (focusedElement?.Name == "HHotkeyTextBox")
            {
                _hModifiers = CaptureModifiers();
                _hKey = KeyInterop.VirtualKeyFromKey(key);
                HHotkeyTextBox.Text = FormatHotkeyText(_hModifiers, _hKey);
                e.Handled = true;
            }

            if (focusedElement?.Name == "SHotkeyTextBox")
            {
                _sModifiers = CaptureModifiers();
                _sKey = KeyInterop.VirtualKeyFromKey(key);
                SHotkeyTextBox.Text = FormatHotkeyText(_sModifiers, _sKey);
                e.Handled = true;
            }

            if (focusedElement?.Name == "PHotkeyTextBox")
            {
                _pModifiers = CaptureModifiers();
                _pKey = KeyInterop.VirtualKeyFromKey(key);
                PHotkeyTextBox.Text = FormatHotkeyText(_pModifiers, _pKey);
                e.Handled = true;
            }
        }

        private static int CaptureModifiers()
        {
            int modifiers = 0;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                modifiers |= HotKeyNativeMethods.MOD_CONTROL;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                modifiers |= HotKeyNativeMethods.MOD_ALT;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                modifiers |= HotKeyNativeMethods.MOD_SHIFT;
            return modifiers;
        }

        private static string FormatHotkeyText(int modifiers, int key)
        {
            if (key == 0)
                return "未设置";

            var parts = new List<string>();

            if ((modifiers & HotKeyNativeMethods.MOD_CONTROL) != 0)
                parts.Add("Ctrl");
            if ((modifiers & HotKeyNativeMethods.MOD_ALT) != 0)
                parts.Add("Alt");
            if ((modifiers & HotKeyNativeMethods.MOD_SHIFT) != 0)
                parts.Add("Shift");

            string keyName = KeyInterop.KeyFromVirtualKey(key).ToString();
            parts.Add(keyName);

            return string.Join(" + ", parts);
        }

        #endregion

        #region 保存 / 重置

        private void HSave_Click(object sender, RoutedEventArgs e)
        {
            var cfg = HotkeySettingsStore.Load();
            cfg.HideModifiers = _hModifiers;
            cfg.HideKey = _hKey;
            HotkeySettingsStore.Save(cfg);

            MessageBox.Show("隐藏热键设置已保存，重启程序后生效");
        }

        private void SSave_Click(object sender, RoutedEventArgs e)
        {
            var cfg = HotkeySettingsStore.Load();
            cfg.SwitchModifiers = _sModifiers;
            cfg.SwitchKey = _sKey;
            HotkeySettingsStore.Save(cfg);

            MessageBox.Show("切换热键设置已保存，重启程序后生效");
        }

        private void HReset_Click(object sender, RoutedEventArgs e)
        {
            _hModifiers = 0;
            _hKey = 0;
            var cfg = HotkeySettingsStore.Load();
            cfg.HideModifiers = 0;
            cfg.HideKey = 0;
            HotkeySettingsStore.Save(cfg);
            HHotkeyTextBox.Text = "未设置";
        }

        private void SReset_Click(object sender, RoutedEventArgs e)
        {
            _sModifiers = 0;
            _sKey = 0;
            var cfg = HotkeySettingsStore.Load();
            cfg.SwitchModifiers = 0;
            cfg.SwitchKey = 0;
            HotkeySettingsStore.Save(cfg);
            SHotkeyTextBox.Text = "未设置";
        }

        private void PSave_Click(object sender, RoutedEventArgs e)
        {
            var cfg = HotkeySettingsStore.Load();
            cfg.PluginModifiers = _pModifiers;
            cfg.PluginKey = _pKey;
            HotkeySettingsStore.Save(cfg);

            MessageBox.Show("插件热键设置已保存，重启程序后生效");
        }

        private void PReset_Click(object sender, RoutedEventArgs e)
        {
            _pModifiers = 0;
            _pKey = 0;
            var cfg = HotkeySettingsStore.Load();
            cfg.PluginModifiers = 0;
            cfg.PluginKey = 0;
            HotkeySettingsStore.Save(cfg);
            PHotkeyTextBox.Text = "未设置";
        }

        #endregion
    }
}
