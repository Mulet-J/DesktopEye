using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Services.TTS;
using SoundFlow.Components;
using SoundFlow.Providers;

namespace DesktopEye.Common.Application.Views.Controls
{
    public partial class AudioPlayerControl : UserControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<AudioPlayerControl, string>(nameof(Text));

        public static readonly StyledProperty<Language?> LanguageProperty =
            AvaloniaProperty.Register<AudioPlayerControl, Language?>(nameof(Language));

        public static readonly StyledProperty<ITtsService> TtsManagerProperty =
            AvaloniaProperty.Register<AudioPlayerControl, ITtsService>(nameof(TtsService));

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

        public ITtsService TtsService
        {
            get => GetValue(TtsManagerProperty);
            set => SetValue(TtsManagerProperty, value);
        }

        public AudioPlayerControl()
        {
            InitializeComponent();
            DataContext = new AudioPlayerViewModel();
    
            // Forcer la synchronisation des propriétés au chargement
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SyncProperties();
        }

        private void SyncProperties()
        {
            if (DataContext is AudioPlayerViewModel viewModel)
            {
                viewModel.Text = Text;
                viewModel.Language = Language;
                viewModel.TtsService = TtsService;
        
                Console.WriteLine($"Synchronisation forcée - Text: '{viewModel.Text}', Language: {viewModel.Language}, TtsManager: {viewModel.TtsService != null}");
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
                    Console.WriteLine($"Propriété Text changée dans contrôle : '{newText}'");
                    viewModel.Text = newText;
                }
                else if (change.Property == LanguageProperty)
                {
                    var newLanguage = change.NewValue as Language?;
                    Console.WriteLine($"Propriété Language changée dans contrôle : {newLanguage}");
                    viewModel.Language = newLanguage;
                }
                else if (change.Property == TtsManagerProperty)
                {
                    var newTtsManager = change.NewValue as ITtsService;
                    Console.WriteLine($"Propriété TtsManager changée dans contrôle : {newTtsManager != null}");
                    viewModel.TtsService = newTtsManager;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
    
            // Forcer la synchronisation des propriétés
            if (DataContext is AudioPlayerViewModel viewModel)
            {
                viewModel.Text = Text;
                viewModel.Language = Language;
                viewModel.TtsService = TtsService;
        
                Console.WriteLine($"DataContext initialisé - Text: '{viewModel.Text}', Language: {viewModel.Language}, TtsManager: {viewModel.TtsService != null}");
            }
        }
    }
}