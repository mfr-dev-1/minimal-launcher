namespace Launcher.Core.Workflows;

public sealed class PowerModeCommandParser_c
{
    public ParsedCommand_c Parse(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return ParsedCommand_c.NotCommand();
        }

        if (rawInput.StartsWith('!'))
        {
            return ParseMetaCommand_c(rawInput);
        }

        if (!rawInput.StartsWith('>'))
        {
            return ParsedCommand_c.NotCommand();
        }

        var trimmed = rawInput[1..].Trim();
        if (trimmed.Length == 0)
        {
            return ParsedCommand_c.Invalid("Empty command.");
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && string.Equals(parts[^1], "wterm", StringComparison.OrdinalIgnoreCase))
        {
            var projectName = string.Join(' ', parts.Take(parts.Length - 1));
            return ParsedCommand_c.WindowsTerminal(projectName);
        }

        return ParsedCommand_c.Invalid("Not implemented command.");
    }

    private static ParsedCommand_c ParseMetaCommand_c(string rawInput)
    {
        var meta = rawInput[1..].Trim();
        if (meta.Length == 0)
        {
            return ParsedCommand_c.Invalid("Empty meta command.");
        }

        if (string.Equals(meta, "exit", StringComparison.OrdinalIgnoreCase))
        {
            return ParsedCommand_c.MetaExit();
        }

        if (string.Equals(meta, "settings", StringComparison.OrdinalIgnoreCase))
        {
            return ParsedCommand_c.MetaConfig();
        }

        if (string.Equals(meta, "refresh", StringComparison.OrdinalIgnoreCase))
        {
            return ParsedCommand_c.MetaRefresh();
        }

        if (string.Equals(meta, "index", StringComparison.OrdinalIgnoreCase))
        {
            return ParsedCommand_c.MetaIndex();
        }

        if (string.Equals(meta, "onboarding", StringComparison.OrdinalIgnoreCase))
        {
            return ParsedCommand_c.MetaOnboarding();
        }

        return ParsedCommand_c.Invalid("Not implemented meta command.");
    }

}

public sealed class ParsedCommand_c
{
    public bool IsCommand { get; private init; }

    public bool IsValid { get; private init; }

    public bool IsWindowsTerminal { get; private init; }

    public bool IsMetaExit { get; private init; }

    public bool IsMetaConfig { get; private init; }

    public bool IsMetaRefresh { get; private init; }

    public bool IsMetaIndex { get; private init; }

    public bool IsMetaOnboarding { get; private init; }

    public string ProjectHint { get; private init; } = string.Empty;

    public string Error { get; private init; } = string.Empty;

    public static ParsedCommand_c NotCommand()
    {
        return new ParsedCommand_c { IsCommand = false, IsValid = false };
    }

    public static ParsedCommand_c Invalid(string error)
    {
        return new ParsedCommand_c { IsCommand = true, IsValid = false, Error = error };
    }

    public static ParsedCommand_c WindowsTerminal(string projectHint)
    {
        return new ParsedCommand_c
        {
            IsCommand = true,
            IsValid = true,
            IsWindowsTerminal = true,
            ProjectHint = projectHint
        };
    }

    public static ParsedCommand_c MetaExit()
    {
        return new ParsedCommand_c
        {
            IsCommand = true,
            IsValid = true,
            IsMetaExit = true
        };
    }

    public static ParsedCommand_c MetaConfig()
    {
        return new ParsedCommand_c
        {
            IsCommand = true,
            IsValid = true,
            IsMetaConfig = true
        };
    }

    public static ParsedCommand_c MetaRefresh()
    {
        return new ParsedCommand_c
        {
            IsCommand = true,
            IsValid = true,
            IsMetaRefresh = true
        };
    }

    public static ParsedCommand_c MetaIndex()
    {
        return new ParsedCommand_c
        {
            IsCommand = true,
            IsValid = true,
            IsMetaIndex = true
        };
    }

    public static ParsedCommand_c MetaOnboarding()
    {
        return new ParsedCommand_c
        {
            IsCommand = true,
            IsValid = true,
            IsMetaOnboarding = true
        };
    }
}
