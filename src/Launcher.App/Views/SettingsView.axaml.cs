using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public void FocusPrimary()
    {
        SettingsGeneralExitTextBox.Focus();
        SettingsGeneralExitTextBox.SelectAll();
    }

    private void OnSettingsSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.SaveSettings();
    }

    private void OnSettingsCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.CancelSettings();
    }

    private void OnSettingsCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.CancelSettings();
    }
}
