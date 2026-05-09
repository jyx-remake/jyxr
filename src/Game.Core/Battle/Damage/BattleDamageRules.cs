namespace Game.Core.Battle;

public static class BattleDamageRules
{
    public const double FriendlyFireDamageMultiplier = 0.25d;

    public static double GetSkillDamageMultiplier(BattleUnit source, BattleUnit target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (string.Equals(source.Id, target.Id, StringComparison.Ordinal))
        {
            return 0d;
        }

        return source.Team == target.Team
            ? FriendlyFireDamageMultiplier
            : 1d;
    }
}
