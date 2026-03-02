using System.ComponentModel;

namespace Launcher.App.ViewModels;

public sealed class OnboardingRootItem_c : INotifyPropertyChanged
{
    public string Path { get; init; } = string.Empty;

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
