using Game.Core.Abstractions;
using Game.Core.Affix;

namespace Game.Core.Battle;

internal static class BattleRageGainResolver
{
    public static void TryGain(
        BattleState state,
        BattleUnit unit,
        IRandomService random,
        HookTiming? timing,
        string detailSource)
    {
        var chance = 0.5d + unit.GetStat(StatType.Fuyuan) / 1000d;
        if (unit.HasTrait(TraitId.Irascible))
        {
            chance += 0.15d;
        }

        if (!Probability.RollChance(random, chance))
        {
            return;
        }

        var amount = unit.HasTrait(TraitId.DoubleCombatRageGain) ? 2 : 1;
        BattleResourceResolver.AddRage(state, unit, amount, timing, detailSource);
    }
}
