﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopEye.Views.Launcher;

public partial class LauncherView : UserControl
{
    public LauncherView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}