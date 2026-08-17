using QiDian.Plugins.Translate;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QiDian.Plugins.Translate
{
    /// <summary>
    /// TranslateView.xaml 的交互逻辑
    /// </summary>
    public partial class TranslateView : UserControl,IViewFor<TranslateViewModel>
    {
        public TranslateView()
        {
            InitializeComponent();
        }

        #region ViewModel

        public TranslateViewModel? ViewModel
        {
            get => (TranslateViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TranslateViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(TranslateViewModel), typeof(TranslateView),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is TranslateView view)
                        view.DataContext = e.NewValue;
                }));

        #endregion
    }
}
