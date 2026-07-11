using Game.Core.Affix;

namespace Game.Core.Battle;

internal static class BattleHookPreviewPolicy
{
    public static void EnsureSafe(BattleHookContext context, HookAffix hook)
    {
        if (!context.IsPreview)
        {
            return;
        }

        if (hook.Speech is not null)
        {
            throw Unsupported(context, "speech");
        }

        if (hook.Conditions.Any(static condition => condition is ChanceBattleHookConditionDefinition))
        {
            throw Unsupported(context, "random chance conditions");
        }

        foreach (var effect in hook.Effects)
        {
            switch (effect)
            {
                case ModifyDamageBattleHookEffectDefinition:
                case ModifyDamageContextBattleHookEffectDefinition:
                case ModifyMpCostBattleHookEffectDefinition:
                case ModifyRecoveryBattleHookEffectDefinition:
                case CustomBattleEffectDefinition { SupportsPreview: true }:
                    break;
                case CustomBattleEffectDefinition:
                    throw Unsupported(context, $"side-effect effect '{effect.GetType().Name}'");
                case StrengthenContextBuffBattleHookEffectDefinition:
                case ApplyBuffBattleEffectDefinition:
                case RemoveBuffBattleEffectDefinition:
                case RemoveNegativeBuffsBattleEffectDefinition:
                case RemovePositiveBuffsBattleEffectDefinition:
                case AddRageBattleEffectDefinition:
                case SetRageBattleEffectDefinition:
                case SetActionGaugeBattleEffectDefinition:
                case AddHpBattleEffectDefinition:
                case AddMpBattleEffectDefinition:
                case CancelHitBattleHookEffectDefinition:
                case SetHitSuccessBattleHookEffectDefinition:
                case ExtraStrikeBattleHookEffectDefinition:
                    throw Unsupported(context, $"side-effect effect '{effect.GetType().Name}'");
                default:
                    throw new NotSupportedException($"Unsupported preview battle hook effect '{effect.GetType().Name}'.");
            }
        }
    }

    private static InvalidOperationException Unsupported(BattleHookContext context, string feature) =>
        new($"Preview battle hook execution does not support {feature} on timing '{context.Timing}'.");
}
