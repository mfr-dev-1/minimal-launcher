using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Launcher.App.ViewModels;

public abstract class ViewModelBase_c : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty_c<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged_c(propertyName);
        return true;
    }

    protected void OnPropertyChanged_c([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
