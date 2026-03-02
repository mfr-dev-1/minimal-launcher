using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Launcher.App.Views;

public partial class ResultsView : UserControl
{
    public ResultsView()
    {
        InitializeComponent();
    }

    public void ScrollToSelected()
    {
        if (ResultList.SelectedItem is null) return;
        ResultList.ScrollIntoView(ResultList.SelectedItem);
    }

    public TranslateTransform EnsureAiShakeTransform()
    {
        if (AiResponsePanel.RenderTransform is TranslateTransform t) return t;
        var next = new TranslateTransform();
        AiResponsePanel.RenderTransform = next;
        return next;
    }

    private async void OnResultListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        if (vm.IsMetaCommandMode || vm.IsProjectSearchMode)
            await vm.HandleConfirmAsync(KeyModifiers.None);
    }
}
