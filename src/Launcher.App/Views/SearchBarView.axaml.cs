using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.App.Views;

public partial class SearchBarView : UserControl
{
    public SearchBarView()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        SearchBox.Focus();
        var text = SearchBox.Text ?? string.Empty;
        SearchBox.CaretIndex = text.Length > 0 && (text[0] == '>' || text[0] == '?') ? 1 : text.Length;
    }

    public void SetBusyRejectActive(bool active)
    {
        if (active) SearchBarShell.Classes.Add("busyReject");
        else SearchBarShell.Classes.Remove("busyReject");
    }

    private async void OnTopHintProjectsClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        await vm.NavigateToProjectModeAsync();
        FocusSearch();
    }

    private async void OnTopHintTerminalClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        await vm.NavigateToTerminalModeAsync();
        FocusSearch();
    }

    private void OnTopHintMetaClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.NavigateToMetaMode();
        FocusSearch();
    }

    private async void OnTopHintAiClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        await vm.NavigateToAiModeAsync();
        FocusSearch();
    }

    private async void OnClearTerminalClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        await vm.ClearTerminalOutputAsync();
        FocusSearch();
    }

    private void OnSettingsOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.OpenSettings();
    }
}
