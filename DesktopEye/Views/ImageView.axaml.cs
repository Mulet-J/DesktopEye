using Avalonia.Controls;
using DesktopEye.ViewModels;

namespace DesktopEye.Views;

public partial class ImageView : UserControl
{
    public ImageView()
    {
        InitializeComponent();
    }

    private void OnSelectionConfirmed()
    {
        if (DataContext is ImageViewModel viewModel)
            viewModel.ProcessSelectionCommand.Execute(this);
    }
}