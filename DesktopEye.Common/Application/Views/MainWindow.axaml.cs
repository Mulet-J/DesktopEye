using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopEye.Common.Application.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.FindControl<MainView>("MainView");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}