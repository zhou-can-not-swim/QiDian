using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.Home
{
    public partial class HomeView : UserControl, IViewFor<HomeViewModel>
    {
        public HomeView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                // 绑定逻辑在此注册
            });
        }

        #region ViewModel

        public HomeViewModel? ViewModel
        {
            get => (HomeViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (HomeViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(HomeViewModel), typeof(HomeView),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is HomeView view)
                        view.DataContext = e.NewValue;
                }));

        #endregion
    }
}
