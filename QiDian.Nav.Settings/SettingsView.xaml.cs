using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.Settings
{
    public partial class SettingsView : UserControl, IViewFor<SettingsViewModel>
    {
        public SettingsView()
        {
            InitializeComponent();

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
    }
}
