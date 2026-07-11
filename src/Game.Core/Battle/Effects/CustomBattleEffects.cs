using System.Text.Json;
using Game.Core.Serialization;
using Game.Core.Battle.Talents;
using Game.Core.Battle.SpecialSkills;
using Game.Core.Battle.Buffs;
using Game.Core.Affix;

namespace Game.Core.Battle;

public abstract class CustomBattleEffectHandler<TParameters, TContext>
    where TContext : class, IBattleEffectContext
{
    public virtual bool SupportsPreview => false;

    public abstract IReadOnlySet<HookTiming> SupportedTimings { get; }

    public virtual void Validate(TParameters parameters)
    {
    }

    public abstract void Execute(TContext context, TParameters parameters);
}

public abstract class CustomAbilityBattleEffectHandler<TParameters>
{
    public virtual void Validate(TParameters parameters)
    {
    }

    public abstract void Execute(IBattleAbilityEffectContext context, TParameters parameters);
}

public sealed class CustomBattleEffectRegistry
{
    private readonly Dictionary<string, ICustomBattleEffectHandler> _handlers = new(StringComparer.Ordinal);

    private CustomBattleEffectRegistry()
    {
    }

    public static CustomBattleEffectRegistry Default { get; } = CreateDefault();

    public void Register<TParameters, TContext>(
        string effectId,
        CustomBattleEffectHandler<TParameters, TContext> handler)
        where TContext : class, IBattleEffectContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(effectId, new CustomBattleEffectHandlerAdapter<TParameters, TContext>(handler)))
        {
            throw new InvalidOperationException($"Custom battle effect '{effectId}' is already registered.");
        }
    }

    public void Register<TParameters>(
        string effectId,
        CustomAbilityBattleEffectHandler<TParameters> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(effectId, new CustomAbilityBattleEffectHandlerAdapter<TParameters>(handler)))
        {
            throw new InvalidOperationException($"Custom battle effect '{effectId}' is already registered.");
        }
    }

    internal CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectId);
        if (!_handlers.TryGetValue(effectId, out var handler))
        {
            throw new InvalidOperationException($"Custom battle effect '{effectId}' is not registered.");
        }

        return handler.Bind(effectId, parameters);
    }

    private static CustomBattleEffectRegistry CreateDefault()
    {
        var registry = new CustomBattleEffectRegistry();
        registry.Register("illness", new IllnessBattleEffectHandler());
        registry.Register("drunkenness", new DrunkennessBattleEffectHandler());
        registry.Register("bright_sacred_fire_formation", new BrightSacredFireFormationBattleEffectHandler());
        registry.Register("weapon_equipment_penalty", new WeaponEquipmentPenaltyBattleEffectHandler());
        registry.Register("zhenwu_formation_attack", new ZhenwuFormationAttackBattleEffectHandler());
        registry.Register("zhenwu_formation_intercept", new ZhenwuFormationInterceptBattleEffectHandler());
        registry.Register("careful_defense", new CarefulDefenseBattleEffectHandler());
        registry.Register("shifting_stars_reflection", new ShiftingStarsReflectionBattleEffectHandler());
        registry.Register("eternal_spring", new EternalSpringBattleEffectHandler());
        registry.Register("survive_at_one_hp", new SurviveAtOneHpBattleEffectHandler());
        registry.Register("qi_shield_defeat_prevention", new QiShieldDefeatPreventionBattleEffectHandler());
        registry.Register("endless_fighting_spirit", new EndlessFightingSpiritBattleEffectHandler());
        registry.Register("xiang_er_wish_damage", new XiangErWishDamageBattleEffectHandler());
        registry.Register("firearm_damage", new FirearmDamageBattleEffectHandler());
        registry.Register("shoulder_throw", new ShoulderThrowBattleEffectHandler());
        registry.Register("dragon_tail_sweep", new DragonTailSweepBattleEffectHandler());
        registry.Register("huatuo_reborn_healing", new HuatuoRebornHealingBattleEffectHandler());
        registry.Register("sever_in_two", new SeverInTwoBattleEffectHandler());
        registry.Register("mutual_destruction_sword", new MutualDestructionSwordBattleEffectHandler());
        registry.Register("heaven_and_earth_same_lifespan", new HeavenAndEarthSameLifespanBattleEffectHandler());
        registry.Register("formless_healing", new FormlessHealingBattleEffectHandler());
        registry.Register("toad_mastery_attack", new ToadMasteryAttackBattleEffectHandler());
        registry.Register("attribute_contest_debuff", new AttributeContestDebuffBattleEffectHandler());
        registry.Register("soul_chasing", new SoulChasingBattleEffectHandler());
        registry.Register("medical_immortal", new MedicalImmortalBattleEffectHandler());
        registry.Register("heal_the_wounded", new HealTheWoundedBattleEffectHandler());
        registry.Register("flying_skyward", new FlyingSkywardBattleEffectHandler());
        registry.Register("life_and_death", new LifeAndDeathBattleEffectHandler());
        registry.Register("cloud_climbing_dragon", new CloudClimbingDragonBattleEffectHandler());
        registry.Register("formless_sword_qi", new FormlessSwordQiBattleEffectHandler());
        registry.Register("internal_energy_attack", new InternalEnergyAttackBattleEffectHandler());
        registry.Register("grieving_breeze", new GrievingBreezeBattleEffectHandler());
        registry.Register("beloved_companion_damage", new BelovedCompanionDamageBattleEffectHandler());
        registry.Register("low_health_target_damage", new LowHealthTargetDamageBattleEffectHandler());
        registry.Register("rage_damage", new RageDamageBattleEffectHandler());
        registry.Register("poisoned_target_level_damage", new PoisonedTargetLevelDamageBattleEffectHandler());
        registry.Register("reset_skill_cooldowns", new ResetSkillCooldownsBattleEffectHandler());
        registry.Register("random_debuff", new RandomDebuffBattleEffectHandler());
        registry.Register("random_strengthen_context_buff", new RandomStrengthenContextBuffBattleEffectHandler());
        return registry;
    }

    private interface ICustomBattleEffectHandler
    {
        CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters);
    }

    private sealed class CustomBattleEffectHandlerAdapter<TParameters, TContext>(
        CustomBattleEffectHandler<TParameters, TContext> handler) : ICustomBattleEffectHandler
        where TContext : class, IBattleEffectContext
    {
        public CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters)
        {
            foreach (var timing in handler.SupportedTimings)
            {
                if (!BattleEffectCapabilityPolicy.Supports<TContext>(timing))
                {
                    throw new InvalidOperationException(
                        $"Custom battle effect '{effectId}' cannot use capability '{typeof(TContext).Name}' at timing '{timing}'.");
                }
            }

            TParameters parsedParameters;
            try
            {
                parsedParameters = parameters.Deserialize<TParameters>(GameJson.Default)
                    ?? throw new InvalidOperationException($"Custom battle effect '{effectId}' parameters cannot be null.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Custom battle effect '{effectId}' parameters are invalid.",
                    exception);
            }

            BattleEffectParameterValidation.Validate(effectId, parsedParameters);
            handler.Validate(parsedParameters);
            return new CustomBattleEffectInvocation(
                handler.SupportsPreview,
                handler.SupportedTimings,
                ExecuteHook: context => handler.Execute(
                    context as TContext ?? throw new InvalidOperationException(
                        $"Custom battle effect '{effectId}' requires capability '{typeof(TContext).Name}'."),
                    parsedParameters),
                ExecuteAbility: null);
        }
    }

    private sealed class CustomAbilityBattleEffectHandlerAdapter<TParameters>(
        CustomAbilityBattleEffectHandler<TParameters> handler) : ICustomBattleEffectHandler
    {
        public CustomBattleEffectInvocation Bind(string effectId, JsonElement parameters)
        {
            TParameters parsedParameters;
            try
            {
                parsedParameters = parameters.Deserialize<TParameters>(GameJson.Default)
                    ?? throw new InvalidOperationException($"Custom battle effect '{effectId}' parameters cannot be null.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Custom battle effect '{effectId}' parameters are invalid.",
                    exception);
            }

            BattleEffectParameterValidation.Validate(effectId, parsedParameters);
            handler.Validate(parsedParameters);
            return new CustomBattleEffectInvocation(
                SupportsPreview: false,
                SupportedTimings: new HashSet<HookTiming>(),
                ExecuteHook: null,
                ExecuteAbility: context => handler.Execute(context, parsedParameters));
        }
    }
}

internal sealed record CustomBattleEffectInvocation(
    bool SupportsPreview,
    IReadOnlySet<HookTiming> SupportedTimings,
    Action<BattleHookContext>? ExecuteHook,
    Action<IBattleAbilityEffectContext>? ExecuteAbility)
{
    public bool SupportsAbility => ExecuteAbility is not null;
}

internal static class BattleEffectCapabilityPolicy
{
    private static readonly IReadOnlyDictionary<Type, HookTiming> SupportedTimings =
        new Dictionary<Type, HookTiming>
        {
            [typeof(IDamageCalculationEffectContext)] = HookTiming.BeforeDamageCalculation,
            [typeof(IHitResultEffectContext)] = HookTiming.BeforeHitResolved,
            [typeof(IHitConfirmedEffectContext)] = HookTiming.OnHitConfirmed,
            [typeof(IDamageApplicationEffectContext)] = HookTiming.BeforeDamageApplied,
            [typeof(IDamageApplicationRuntimeContext)] = HookTiming.BeforeDamageApplied,
            [typeof(IDamageTakenEffectContext)] = HookTiming.OnDamageTaken,
            [typeof(IDamageDealtEffectContext)] = HookTiming.OnDamageDealt,
            [typeof(IDefeatPreventionEffectContext)] = HookTiming.BeforeDefeated,
            [typeof(IRecoveryEffectContext)] = HookTiming.BeforeRecoveryResolved,
            [typeof(ISkillCostEffectContext)] = HookTiming.BeforeSkillCost,
            [typeof(IBuffApplicationEffectContext)] = HookTiming.BeforeBuffApplied,
            [typeof(IActionStartEffectContext)] = HookTiming.BeforeActionStart,
            [typeof(IActionReadinessEffectContext)] = HookTiming.BeforeActionReadiness,
        };

    public static bool Supports<TContext>(HookTiming timing)
        where TContext : class, IBattleEffectContext
    {
        var contextType = typeof(TContext);
        return contextType == typeof(IBattleEffectContext) ||
            SupportedTimings.TryGetValue(contextType, out var supportedTiming) && supportedTiming == timing;
    }
}
