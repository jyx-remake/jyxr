using Game.Core.Affix;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal sealed class BattleEffectExecutor(BattleEngine engine)
{
    public void ExecuteHook(BattleHookContext context, BattleEffectDefinition effect) =>
        Execute(context.State, context.Unit, context.Source ?? context.Unit,
            context.Target is null ? [] : [context.Target], effect, context.Timing, context);

    public void ExecuteAbility(
        BattleState state,
        BattleUnit source,
        IReadOnlyList<BattleUnit> primaryTargets,
        BattleEffectDefinition effect,
        SkillInstance skill)
    {
        if (effect is CustomAbilityBattleEffectDefinition custom)
        {
            var targets = BattleTargetResolver.Resolve(
                state,
                source,
                source,
                primaryTargets,
                custom.Target);
            custom.ExecuteAbility(new BattleAbilityEffectContext(
                engine,
                state,
                source,
                targets,
                skill,
                engine.RandomService));
            return;
        }

        Execute(state, source, source, primaryTargets, effect, null, null);
    }

    private void Execute(
        BattleState state,
        BattleUnit contextUnit,
        BattleUnit source,
        IReadOnlyList<BattleUnit> primaryTargets,
        BattleEffectDefinition effect,
        HookTiming? timing,
        BattleHookContext? hookContext)
    {
        var targets = effect is ITargetedBattleEffectDefinition targeted
            ? BattleTargetResolver.Resolve(state, contextUnit, source, primaryTargets, targeted.Target)
            : [];

        switch (effect)
        {
            case ModifyDamageBattleHookEffectDefinition modify:
                RequireHook().DamageAmount = ApplyModifier(RequireHook().DamageAmount, RequireHook(), modify.Op,
                    modify.Delta, modify.DeltaPerBuffLevel, modify.Rounding);
                break;
            case ModifyDamageContextBattleHookEffectDefinition modifyContext:
                ApplyDamageContextModifier(RequireHook(), modifyContext);
                break;
            case ModifyMpCostBattleHookEffectDefinition modifyCost:
                RequireHook().MpCost = ApplyModifier(RequireHook().MpCost, RequireHook(), modifyCost.Op,
                    modifyCost.Delta, modifyCost.DeltaPerBuffLevel, modifyCost.Rounding);
                break;
            case ModifyRecoveryBattleHookEffectDefinition modifyRecovery:
                RequireHook().RecoveryAmount = ApplyModifier(RequireHook().RecoveryAmount, RequireHook(), modifyRecovery.Op,
                    modifyRecovery.Delta, modifyRecovery.DeltaPerBuffLevel, modifyRecovery.Rounding);
                break;
            case StrengthenContextBuffBattleHookEffectDefinition strengthen:
                (RequireHook().Buff ?? throw new InvalidOperationException("Battle effect requires a context buff."))
                    .Strengthen(strengthen.LevelDelta, strengthen.TurnDelta);
                break;
            case ApplyBuffBattleEffectDefinition applyBuff:
                foreach (var target in targets)
                {
                    if (Probability.RollPercentage(hookContext?.Random ?? engine.RandomService, applyBuff.Chance))
                    {
                        engine.BuffResolver.Apply(state, source, target, engine.BuffResolver.Resolve(applyBuff), applyBuff.Level,
                            applyBuff.Duration, timing);
                    }
                }
                break;
            case RemoveBuffBattleEffectDefinition remove:
                foreach (var target in targets)
                    engine.BuffResolver.Remove(state, source, target,
                        buff => string.Equals(buff.Definition.Id, remove.BuffId, StringComparison.Ordinal), timing);
                break;
            case RemoveNegativeBuffsBattleEffectDefinition:
                foreach (var target in targets)
                    engine.BuffResolver.Remove(state, source, target, buff => buff.Definition.IsDebuff, timing);
                break;
            case RemovePositiveBuffsBattleEffectDefinition:
                foreach (var target in targets)
                    engine.BuffResolver.Remove(state, source, target, buff => !buff.Definition.IsDebuff, timing);
                break;
            case RemoveContextBuffBattleEffectDefinition:
                var context = RequireHook();
                var contextBuff = context.Buff
                    ?? throw new InvalidOperationException("Battle effect requires a context buff.");
                if (!contextBuff.IsExpired)
                {
                    engine.BuffResolver.Remove(
                        state,
                        source,
                        context.Unit,
                        buff => ReferenceEquals(buff, contextBuff),
                        timing);
                }
                break;
            case AddRageBattleEffectDefinition addRage:
                foreach (var target in targets) BattleResourceResolver.AddRage(state, target, addRage.Value, timing);
                break;
            case SetRageBattleEffectDefinition setRage:
                foreach (var target in targets) BattleResourceResolver.SetRage(state, target, setRage.Value, timing);
                break;
            case AddActionGaugeBattleEffectDefinition addGauge:
                foreach (var target in targets) BattleResourceResolver.AddActionGauge(state, target, addGauge.Value, timing);
                break;
            case SetActionGaugeBattleEffectDefinition gauge:
                foreach (var target in targets) BattleResourceResolver.SetActionGauge(target, gauge.Value);
                break;
            case AddHpBattleEffectDefinition hp:
                foreach (var target in targets)
                {
                    var actual = engine.RecoveryResolver
                        .Apply(state, source, target, BattleRecoveryKind.Hp, hp.Value)
                        .ActualAmount;
                    state.AddMessage(new BattleFact(BattleFactKind.Healed, target.Id, timing, detail: actual.ToString()));
                }
                break;
            case AddMpBattleEffectDefinition mp:
                foreach (var target in targets)
                    engine.RecoveryResolver.Apply(state, source, target, BattleRecoveryKind.Mp, mp.Value);
                break;
            case ModifyLifestealBattleHookEffectDefinition lifesteal:
                var dealtContext = RequireHook();
                dealtContext.LifestealRate =
                    (dealtContext.LifestealRate ?? 0d) +
                    lifesteal.Factor +
                    lifesteal.FactorPerUnitLevel * dealtContext.Unit.Character.Level;
                break;
            case CancelHitBattleHookEffectDefinition cancel:
                RequireHook().HitState = BattleHitState.Miss;
                RequireHook().DamageAmount = 0;
                RequireHook().SuppressHitEffects = cancel.SuppressHitEffects;
                break;
            case SetHitSuccessBattleHookEffectDefinition:
                RequireHook().HitState = BattleHitState.Hit;
                RequireHook().SuppressHitEffects = false;
                break;
            case ExtraStrikeBattleHookEffectDefinition extra:
                foreach (var target in targets) engine.ApplyHookExtraStrikeEffect(RequireHook(), target, extra);
                break;
            case CustomBattleEffectDefinition custom:
                custom.ExecuteHook(RequireHook());
                break;
            default:
                throw new NotSupportedException($"Unsupported battle effect '{effect.GetType().Name}'.");
        }

        BattleHookContext RequireHook() => hookContext
            ?? throw new InvalidOperationException($"Effect '{effect.GetType().Name}' requires a hook context.");
    }

    private static void ApplyDamageContextModifier(BattleHookContext context, ModifyDamageContextBattleHookEffectDefinition effect)
    {
        var calculation = context.DamageCalculation
            ?? throw new InvalidOperationException("Battle effect requires a damage calculation context.");
        var delta = ResolveDelta(context, effect) + effect.DeltaPerUnitLevel * context.Unit.Character.Level
            + effect.DeltaPerBuffLevel * (context.Buff?.Level ?? 0);
        if (effect.DeltaPowerBasePerBuffLevel is { } powerBase)
            delta *= Math.Pow(powerBase, context.Buff?.Level ?? 0);
        calculation.AddModifier(effect.Field, effect.Op, delta);
    }

    private static double ResolveDelta(BattleHookContext context, ModifyDamageContextBattleHookEffectDefinition effect)
    {
        if (effect.DeltaMin is null || effect.DeltaMax is null) return effect.Delta;
        if (context.IsPreview) return (effect.DeltaMin.Value + effect.DeltaMax.Value) / 2d;
        return effect.DeltaMin.Value + (effect.DeltaMax.Value - effect.DeltaMin.Value) * context.Random.NextDouble();
    }

    private static int? ApplyModifier(int? current, BattleHookContext context, ModifierOp op,
        double delta, double perBuffLevel, BattleHookRounding rounding)
    {
        if (current is null) return null;
        var resolved = delta + perBuffLevel * (context.Buff?.Level ?? 0);
        var value = op switch
        {
            ModifierOp.Add or ModifierOp.PostAdd => current.Value + resolved,
            ModifierOp.Increase => current.Value * (1d + resolved),
            ModifierOp.More => current.Value * resolved,
            ModifierOp.Override => resolved,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
        };
        var rounded = rounding switch
        {
            BattleHookRounding.Truncate => (int)value,
            BattleHookRounding.Floor => (int)Math.Floor(value),
            BattleHookRounding.Ceiling => (int)Math.Ceiling(value),
            BattleHookRounding.Round => (int)Math.Round(value, MidpointRounding.AwayFromZero),
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, null),
        };
        return Math.Max(0, rounded);
    }
}
