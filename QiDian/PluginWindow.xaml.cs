using QiDian.Contracts;
using QiDian.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QiDian
{
    /// <summary>
    /// 内置服务窗口：左侧下拉框选命令（tr:、h: 等），右侧输入参数，
    /// 回车执行并弹出右下角结果窗；本窗口保持打开，可连续输入。
    /// 再按一次插件热键或按 Esc 返回搜索窗口。
    /// </summary>
    public partial class PluginWindow : Window
    {
        private readonly ICommandRouter _commandRouter;
        private readonly IWindowSwitcherService _windowSwitcher;

        public PluginWindow(ICommandRouter commandRouter, IWindowSwitcherService windowSwitcher)
        {
            InitializeComponent();
            _commandRouter = commandRouter;
            _windowSwitcher = windowSwitcher;

            CommandComboBox.ItemsSource = _commandRouter.Plugins;
            CommandComboBox.SelectedIndex = 0;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 每次打开窗口都清空输入并聚焦
            //ArgTextBox.Clear();
            ArgTextBox.Focus();
        }

        private void CommandComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommandComboBox.SelectedItem is ICommandPlugin p)
                HintText.Text = $"按 Enter 执行：{p.Description}";
        }

        private void ArgTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    // 执行命令，窗口保持打开
                    RunCommand();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    // 返回搜索窗口
                    _windowSwitcher.SwitchToSearchWindow();
                    e.Handled = true;
                    break;
            }
        }

        private void RunCommand()
        {
            if (CommandComboBox.SelectedItem is not ICommandPlugin plugin)
                return;

            _commandRouter.Run(plugin.Prefix, ArgTextBox.Text);

            // 结果已弹到右下角，选中输入框文本便于直接输入下一个词
            ArgTextBox.SelectAll();
            ArgTextBox.Focus();
        }
    }
}
