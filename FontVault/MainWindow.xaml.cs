using FontVault.Models;
using FontVault.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FontVault
{
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<LoadedFont> Fonts { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Title = "FontVault";

            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            IntPtr windowHandle = WindowNative.GetWindowHandle(this);

            WindowId windowId =
                Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);

            AppWindow appWindow =
                AppWindow.GetFromWindowId(windowId);

            appWindow.Resize(new SizeInt32
            {
                Width = 900,
                Height = 620
            });

            appWindow.SetPresenter(
                AppWindowPresenterKind.Overlapped);

            Closed += MainWindow_Closed;
        }

        private async void LoadFont_Click(
            object sender,
            RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();

            picker.FileTypeFilter.Add(".ttf");
            picker.FileTypeFilter.Add(".otf");

            IntPtr windowHandle =
                WindowNative.GetWindowHandle(this);

            InitializeWithWindow.Initialize(
                picker,
                windowHandle);

            var file = await picker.PickSingleFileAsync();

            if (file is null)
                return;

            string fontPath = file.Path;

            bool alreadyListed = Fonts.Any(font =>
                string.Equals(
                    font.FilePath,
                    fontPath,
                    StringComparison.OrdinalIgnoreCase));

            if (alreadyListed)
            {
                await ShowMessageAsync(
                    "Font already loaded",
                    "This font file is already loaded in FontVault.");
                return;
            }

            bool installPermanently =
                PermanentInstallCheckBox.IsChecked == true;

            if (installPermanently)
            {
                await ShowMessageAsync(
                    "Not implemented yet",
                    "Permanent font installation will be added in the next update.");
                return;
            }

            bool loaded =
                await FontService.LoadTemporaryFontAsync(fontPath);

            if (!loaded)
            {
                await ShowMessageAsync(
                    "Unable to load font",
                    "Windows could not load this font. The file may be invalid, unsupported, or unavailable.");
                return;
            }

            string fallbackName =
                Path.GetFileNameWithoutExtension(file.Name);

            string familyName =
                FontMetadataService.GetFontFamilyName(fontPath)
                ?? fallbackName;


            bool previewAvailable =
                !string.IsNullOrWhiteSpace(familyName) &&
                !familyName.Equals(
                    "Segoe UI",
                    StringComparison.OrdinalIgnoreCase);

            Fonts.Add(new LoadedFont
            {
                Name = fallbackName,
                FamilyName = familyName,
                FilePath = fontPath,
                IsPermanent = false,
                PreviewAvailable = previewAvailable
            });
        }

        private async void UnloadFont_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Tag is not LoadedFont font)
                return;

            if (font.IsPermanent)
            {
                await ShowMessageAsync(
                    "Installed font",
                    "Permanent font removal has not been implemented yet.");
                return;
            }

            button.IsEnabled = false;

            try
            {
                bool unloaded =
                    await FontService.UnloadTemporaryFontAsync(font.FilePath);

                if (!unloaded)
                {
                    await ShowMessageAsync(
                        "Unable to unload font",
                        "Windows could not unload this font. Another application may still be using it.");
                    return;
                }

                button.IsEnabled = true;

                Fonts.Remove(font);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async void UnloadAllFonts_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Fonts.Count == 0)
                return;

            if (sender is Button button)
                button.IsEnabled = false;

            var failedFonts = new List<LoadedFont>();

            foreach (LoadedFont font in Fonts.ToList())
            {
                if (font.IsPermanent)
                    continue;

                bool unloaded =
                    await FontService.UnloadTemporaryFontAsync(font.FilePath);

                if (unloaded)
                {
                    Fonts.Remove(font);
                }
                else
                {
                    failedFonts.Add(font);
                }
            }

            if (sender is Button unloadAllButton)
                unloadAllButton.IsEnabled = true;

            if (failedFonts.Count > 0)
            {
                await ShowMessageAsync(
                    "Some fonts could not be unloaded",
                    $"{failedFonts.Count} font file(s) could not be unloaded.");
            }
        }

        private async void MainWindow_Closed(
            object sender,
            WindowEventArgs args)
        {
            await FontService.UnloadAllTemporaryFontsAsync();
        }

        private async Task ShowMessageAsync(
            string title,
            string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}