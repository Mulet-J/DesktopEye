using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopEye.Services;

namespace DesktopEye.Views
{
    public partial class MainWindow : Window
    {
        private readonly TrayIconManager? _trayIconManager;
        
        public MainWindow() 
        {
            InitializeComponent();
            
            this.FindControl<MainView>("MainView");
            _trayIconManager = new TrayIconManager(this);
            Closing += MainWindow_Closing;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            _trayIconManager?.HideToTray();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            _trayIconManager?.Dispose();
        }
    }
}