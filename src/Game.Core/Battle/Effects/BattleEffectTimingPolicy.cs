using Game.Core.Affix;

namespace Game.Core.Battle;

public static class BattleEffectTimingPolicy
{
    public static bool Supports(HookTiming timing, BattleEffectDefinition effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return effect switch
        {
            ModifyDamageBattleHookEffectDefinition => timing == HookTiming.BeforeDamageApplied,
            ModifyDamageContextBattleHookEffectDefinition => timing == HookTiming.BeforeDamageCalculation,
            ModifyMpCostBattleHookEffectDefinition => timing == HookTiming.BeforeSkillCost,
            ModifyRecoveryBattleHookEffectDefinition => timing == HookTiming.BeforeRecoveryResolved,
            ModifyLifestealBattleHookEffectDefinition => timing == HookTiming.OnDamageDealt,
            StrengthenContextBuffBattleHookEffectDefinition => timing == HookTiming.BeforeBuffApplied,
            CancelHitBattleHookEffectDefinition or SetHitSuccessBattleHookEffectDefinition =>
                timing == HookTiming.BeforeHitResolved,
            ExtraStrikeBattleHookEffectDefinition => timing == HookTiming.OnHitConfirmed,
            RemoveContextBuffBattleEffectDefinition => timing == HookTiming.AfterBuffRound,
            CustomBattleEffectDefinition custom => custom.SupportsTiming(timing),
            _ => true,
        };
    }
}
