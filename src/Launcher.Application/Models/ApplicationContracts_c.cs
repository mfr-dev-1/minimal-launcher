namespace Launcher.Application.Models;

public sealed class StartupState_c
{
    public Launcher.Core.Models.LauncherSettings_c Settings { get; init; } = Launcher.Core.Models.LauncherSettings_c.CreateDefault();

    public int CachedProjectCount { get; init; }

    public int DetectedToolCount { get; init; }

    public bool IsOnboardingPending { get; init; }
}

public sealed class MetaCommandSuggestion_c
{
    public string Command { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed class QueryProjection_c
{
    public bool IsMetaMode { get; init; }

    public IReadOnlyList<Launcher.Core.Models.SearchResult_c> SearchResults { get; init; } = [];

    public IReadOnlyList<MetaCommandSuggestion_c> MetaSuggestions { get; init; } = [];
}

public sealed class LaunchExecutionResult_c
{
    public bool IsSuccess { get; private init; }

    public string Message { get; private init; } = string.Empty;

    public static LaunchExecutionResult_c Success(string message)
    {
        return new LaunchExecutionResult_c { IsSuccess = true, Message = message };
    }

    public static LaunchExecutionResult_c Fail(string message)
    {
        return new LaunchExecutionResult_c { IsSuccess = false, Message = message };
    }
}

public sealed class CommandExecutionResult_c
{
    public bool IsSuccess { get; private init; }

    public bool RequestAppExit { get; private init; }

    public bool RequestOpenOnboarding { get; private init; }

    public bool RequestOpenSettings { get; private init; }

    public string Message { get; private init; } = string.Empty;

    public static CommandExecutionResult_c Success(
        string message,
        bool requestAppExit = false,
        bool requestOpenOnboarding = false,
        bool requestOpenSettings = false)
    {
        return new CommandExecutionResult_c
        {
            IsSuccess = true,
            Message = message,
            RequestAppExit = requestAppExit,
            RequestOpenOnboarding = requestOpenOnboarding,
            RequestOpenSettings = requestOpenSettings
        };
    }

    public static CommandExecutionResult_c Fail(string message)
    {
        return new CommandExecutionResult_c
        {
            IsSuccess = false,
            Message = message
        };
    }
}

public sealed class OnboardingTransitionResult_c
{
    public bool IsSuccess { get; private init; }

    public string Message { get; private init; } = string.Empty;

    public static OnboardingTransitionResult_c Success(string message)
    {
        return new OnboardingTransitionResult_c { IsSuccess = true, Message = message };
    }

    public static OnboardingTransitionResult_c Fail(string message)
    {
        return new OnboardingTransitionResult_c { IsSuccess = false, Message = message };
    }
}

public sealed class TerminalCommandResult_c
{
    public bool IsSuccess { get; init; }

    public string CommandText { get; init; } = string.Empty;

    public string OutputText { get; init; } = string.Empty;

    public string StandardOutputText { get; init; } = string.Empty;

    public string StandardErrorText { get; init; } = string.Empty;

    public int? ExitCode { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public static TerminalCommandResult_c Success(string commandText, string outputText, int exitCode)
    {
        return Success(commandText, outputText, string.Empty, exitCode);
    }

    public static TerminalCommandResult_c Success(
        string commandText,
        string standardOutputText,
        string standardErrorText,
        int exitCode)
    {
        var mergedOutput = string.IsNullOrWhiteSpace(standardErrorText)
            ? standardOutputText
            : string.IsNullOrWhiteSpace(standardOutputText)
                ? standardErrorText
                : $"{standardOutputText.TrimEnd()}{Environment.NewLine}{standardErrorText}";

        return new TerminalCommandResult_c
        {
            IsSuccess = true,
            CommandText = commandText,
            OutputText = mergedOutput,
            StandardOutputText = standardOutputText,
            StandardErrorText = standardErrorText,
            ExitCode = exitCode
        };
    }

    public static TerminalCommandResult_c Fail(string commandText, string errorMessage)
    {
        return new TerminalCommandResult_c
        {
            IsSuccess = false,
            CommandText = commandText,
            ErrorMessage = errorMessage
        };
    }
}

public sealed class AiChatResult_c
{
    public bool IsSuccess { get; init; }

    public string OutputMarkdown { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public static AiChatResult_c Success(string outputMarkdown)
    {
        return new AiChatResult_c
        {
            IsSuccess = true,
            OutputMarkdown = outputMarkdown
        };
    }

    public static AiChatResult_c Fail(string errorMessage)
    {
        return new AiChatResult_c
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
