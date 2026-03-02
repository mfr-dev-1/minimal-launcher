using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Launcher.App.Views;

public partial class FooterView : UserControl
{
    public FooterView()
    {
        InitializeComponent();
    }

    private async void OnIdeOptionClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not string token) return;
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        await vm.HandleIdeOptionClickedAsync(token);
    }
}
