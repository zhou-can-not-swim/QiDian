using QiDian.Nav.DM.ViewModels;
using ReactiveUI;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.DM.Views;

public partial class DMDetailView : UserControl, IViewFor<DMDetailViewModel>
{
    public DMDetailView()
    {
        InitializeComponent();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ViewModel?.Back();

    #region ViewModel

    public DMDetailViewModel? ViewModel
    {
        get => (DMDetailViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (DMDetailViewModel?)value;
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(DMDetailViewModel), typeof(DMDetailView),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is DMDetailView view)
                    view.DataContext = e.NewValue;
            }));

    #endregion
}
