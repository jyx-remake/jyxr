namespace Game.Core.Battle;

public static class BattleCueTextFormatter
{
    public static string? Format(
        string? text,
        BattleUnit? owner = null,
        BattleUnit? source = null,
        BattleUnit? target = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text
            .Replace("{owner}", owner?.Character.Name ?? string.Empty, StringComparison.Ordinal)
            .Replace("{source}", source?.Character.Name ?? string.Empty, StringComparison.Ordinal)
            .Replace("{target}", target?.Character.Name ?? string.Empty, StringComparison.Ordinal);
    }
}
