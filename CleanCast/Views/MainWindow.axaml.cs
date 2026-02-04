using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CleanCast.ViewModels;

namespace CleanCast.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenCastWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var win = new CastWindow { DataContext = vm };
            vm.IsCastWindowOpen = true;
            win.Closed += (s, ev) => vm.IsCastWindowOpen = false;
            win.Show();
        }
    }

    private async void AddFile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var storage = GetTopLevel(this)?.StorageProvider;
            if (storage == null) return;

            var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Video File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Videos")
                    {
                        Patterns = new[] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv" }
                    }
                },
                AllowMultiple = true
            });

            if (result.Count > 0)
            {
                foreach (var file in result)
                {
                    vm.AddFileActionCommand.Execute(file.Path.LocalPath);
                }
            }
        }
    }
}