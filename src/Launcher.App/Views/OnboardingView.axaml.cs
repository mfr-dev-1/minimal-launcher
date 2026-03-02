using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Launcher.App.Views;

public partial class OnboardingView : UserControl
{
    private CancellationTokenSource? _addRootErrorCts;
    private static readonly double[] AddRootShakeOffsets_c = [-10d, 8d, -6d, 4d, -2d, 0d];

    public OnboardingView()
    {
        InitializeComponent();
    }

    public void FocusHotkeyInput()
    {
        OnboardingHotkeyTextBox.Focus();
        OnboardingHotkeyTextBox.SelectAll();
    }

    public void TriggerAddRootErrorShake()
    {
        _ = PlayAddRootErrorFeedbackAsync_c();
    }

    private void OnOnboardingAddRootClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.AddOnboardingRoot();
    }

    private void OnOnboardingClearAllRootsClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.ClearOnboardingRoots();
    }

    private void OnOnboardingDeleteRootClicked(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not Launcher.App.ViewModels.OnboardingRootItem_c item) return;
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.RemoveOnboardingRoot(item);
    }

    private void OnOnboardingAddRootInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.AddOnboardingRoot();
    }

    private void OnOnboardingCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.HandleOnboardingExit();
    }

    private void OnOnboardingFinishClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.CompleteOnboarding();
    }

    private void OnOnboardingSkipClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Launcher.App.ViewModels.MainWindowViewModel_o vm) return;
        vm.SkipOnboarding();
    }

    private async Task PlayAddRootErrorFeedbackAsync_c()
    {
        _addRootErrorCts?.Cancel();
        _addRootErrorCts?.Dispose();
        var cts = new CancellationTokenSource();
        _addRootErrorCts = cts;

        var transform = OnboardingAddRootInput.RenderTransform as TranslateTransform
            ?? new TranslateTransform();
        OnboardingAddRootInput.RenderTransform = transform;
        OnboardingAddRootInput.Classes.Add("fieldError");

        try
        {
            foreach (var offset in AddRootShakeOffsets_c)
            {
                cts.Token.ThrowIfCancellationRequested();
                transform.X = offset;
                await Task.Delay(24, cts.Token);
            }
            await Task.Delay(300, cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_addRootErrorCts, cts))
            {
                transform.X = 0;
                OnboardingAddRootInput.Classes.Remove("fieldError");
                _addRootErrorCts.Dispose();
                _addRootErrorCts = null;
            }
        }
    }
}
