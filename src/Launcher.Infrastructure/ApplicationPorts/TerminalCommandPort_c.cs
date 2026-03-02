namespace Launcher.Infrastructure.ApplicationPorts;

public sealed class TerminalCommandPort_c : Launcher.Application.Ports.ITerminalCommandPort_c
{
    private readonly Launcher.Infrastructure.Launch.ShellTerminal_c _terminal;

    public TerminalCommandPort_c(Launcher.Infrastructure.Launch.ShellTerminal_c terminal)
    {
        _terminal = terminal;
    }

    public Task<Launcher.Application.Models.TerminalCommandResult_c> ExecuteAsync(
        Launcher.Core.Models.TerminalSettings_c settings,
        string commandText,
        CancellationToken cancellationToken)
    {
        return _terminal.ExecuteAsync(settings, commandText, cancellationToken);
    }
}
