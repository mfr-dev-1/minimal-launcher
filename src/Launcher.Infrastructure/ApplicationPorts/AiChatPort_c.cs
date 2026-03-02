namespace Launcher.Infrastructure.ApplicationPorts;

public sealed class AiChatPort_c : Launcher.Application.Ports.IAiChatPort_c
{
    private readonly Launcher.Infrastructure.Launch.AiChatCli_c _innerChatCli;

    public AiChatPort_c(Launcher.Infrastructure.Launch.AiChatCli_c innerChatCli)
    {
        _innerChatCli = innerChatCli;
    }

    public Task<Launcher.Application.Models.AiChatResult_c> ExecuteAsync(
        Launcher.Core.Models.AiChatSettings_c settings,
        string prompt,
        CancellationToken cancellationToken)
    {
        return _innerChatCli.ExecuteAsync(settings, prompt, cancellationToken);
    }
}
