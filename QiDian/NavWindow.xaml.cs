using QiDian.ViewModels;
using System.Windows;

namespace QiDian;

public partial class NavWindow : Window
{
    public NavWindow(NavViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        this.Closing += NavWindow_Closing;
    }

    private void NavWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;

        if (Application.Current is App app)
        {
            app.MinimizeToTray();
        }
    }

}