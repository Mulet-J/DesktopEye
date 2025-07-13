using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DesktopEye.Common.Application.ViewModels;
using DesktopEye.Common.Domain.Models;

namespace DesktopEye.Common.Application.Views.Controls
{
    public partial class AudioPlayerControl : UserControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<AudioPlayerControl, string>(nameof(Text), string.Empty);

        public static readonly StyledProperty<Language?> LanguageProperty =
            AvaloniaProperty.Register<AudioPlayerControl, Language?>(nameof(Language));

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Language? Language
        {
            get => GetValue(LanguageProperty);
            set => SetValue(LanguageProperty, value);
        }

        public AudioPlayerControl()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            SyncProperties();
        }

        private void SyncProperties()
        {
            if (DataContext is AudioPlayerViewModel viewModel)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    viewModel.Text = Text;
                }
                
                if (Language.HasValue)
                {
                    viewModel.Language = Language;
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (DataContext is AudioPlayerViewModel viewModel)
            {
                if (change.Property == TextProperty)
                {
                    var newText = change.NewValue as string;
                    viewModel.Text = newText ?? string.Empty;
                }
                else if (change.Property == LanguageProperty)
                {
                    var newLanguage = (Language?)change.NewValue;
                    viewModel.Language = newLanguage;
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is AudioPlayerViewModel viewModel)
            {
                viewModel.Text = Text;
                viewModel.Language = Language;
            }
        }
    }
}