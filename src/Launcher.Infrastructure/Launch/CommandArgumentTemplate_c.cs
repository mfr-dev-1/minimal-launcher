namespace Launcher.Infrastructure.Launch;

internal static class CommandArgumentTemplate_c
{
    public static List<string> Tokenize(string rawTemplate)
    {
        if (string.IsNullOrWhiteSpace(rawTemplate))
        {
            return [];
        }

        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        char quote = '\0';

        foreach (var ch in rawTemplate)
        {
            if (quote == '\0' && char.IsWhiteSpace(ch))
            {
                FlushToken_c(tokens, current);
                continue;
            }

            if (ch == '"' || ch == '\'')
            {
                if (quote == '\0')
                {
                    quote = ch;
                    continue;
                }

                if (quote == ch)
                {
                    quote = '\0';
                    continue;
                }
            }

            current.Append(ch);
        }

        FlushToken_c(tokens, current);
        return tokens;
    }

    private static void FlushToken_c(List<string> tokens, System.Text.StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        tokens.Add(current.ToString());
        current.Clear();
    }
}
