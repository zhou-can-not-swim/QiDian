using ReactiveUI;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Plugins.Help;

public partial class HelpView : UserControl,IViewFor<HelpViewModel>
{
    public HelpView()
    {
        InitializeComponent();
    }

    #region ViewModel

    public HelpViewModel? ViewModel
    {
        get => (HelpViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (HelpViewModel?)value;
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(HelpViewModel), typeof(HelpView),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is HelpView view)
                    view.DataContext = e.NewValue;
            }));

    #endregion
}
