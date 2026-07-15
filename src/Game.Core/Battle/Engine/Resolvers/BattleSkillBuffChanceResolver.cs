using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Core.Battle;

internal static class BattleSkillBuffChanceResolver
{
    public static double Resolve(
        SkillBuffDefinition buff,
        BattleUnit source,
        BattleUnit target)
    {
        if (buff.Chance is { } explicitChance)
        {
            return Math.Clamp(explicitChance / 100d, 0d, 1d);
        }

        var fortune = source.GetStat(StatType.Fuyuan);
        if (!buff.Buff.IsDebuff)
        {
            return Math.Clamp(fortune / 300d, 0d, 1d);
        }

        var composure = target.GetStat(StatType.Dingli);
        return Math.Clamp(0.3d + (fortune - composure) / 200d, 0.1d, 0.8d);
    }
}
