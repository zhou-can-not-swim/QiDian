using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.DM
{
    public partial class DMView : UserControl, IViewFor<DMViewModel>
    {
        public DMView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                // 绑定逻辑在此注册
            });
        }

        #region ViewModel

        public DMViewModel? ViewModel
        {
            get => (DMViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (DMViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(DMViewModel), typeof(DMView),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is DMView view)
                        view.DataContext = e.NewValue;
                }));

        #endregion
    }
}
