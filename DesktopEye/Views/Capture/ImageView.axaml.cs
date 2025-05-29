using System;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.ViewModels;

namespace DesktopEye.Views.Capture;

public partial class ImageView : UserControl
{
    public ImageView()
    {
        InitializeComponent();
        // KeyDown += OnKeyDown;
        KeyBindings.Add(new KeyBinding
        {
            Command = new RelayCommand(test),
            Gesture = new KeyGesture(Key.Enter)
        });
    }

    private void test()
    {
        Console.WriteLine("azeazezae");
        if (DataContext is ImageViewModel viewmodel) viewmodel.ProcessSelectionCommand.Execute(this);
    }

    private void OnSelectionConfirmed()
    {
        if (DataContext is ImageViewModel viewModel)
            viewModel.ProcessSelectionCommand.Execute(this);
    }
}