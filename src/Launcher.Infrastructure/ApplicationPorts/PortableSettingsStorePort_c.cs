namespace Launcher.Infrastructure.ApplicationPorts;

public sealed class PortableSettingsStorePort_c : Launcher.Application.Ports.ISettingsStorePort_c
{
    private readonly Launcher.Infrastructure.Storage.PortableSettingsStore_c _innerStore;

    public PortableSettingsStorePort_c(Launcher.Infrastructure.Storage.PortableSettingsStore_c innerStore)
    {
        _innerStore = innerStore;
    }

    public string SettingsPath => _innerStore.SettingsPath;

    public Launcher.Core.Models.LauncherSettings_c LoadOrCreate()
    {
        return _innerStore.LoadOrCreate();
    }

    public void Save(Launcher.Core.Models.LauncherSettings_c settings)
    {
        _innerStore.Save(settings);
    }
}
