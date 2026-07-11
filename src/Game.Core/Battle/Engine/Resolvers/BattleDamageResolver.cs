using Game.Core.Affix;
using Game.Core.Model;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal sealed record BattleDamageApplicationResult(
    BattleUnit Target,
    int ActualAmount,
    bool SuppressHitEffects);

internal sealed class BattleDamageApplicationContext(
    BattleUnit source,
    BattleUnit target,
    SkillInstance? skill,
    int proposedAmount,
    bool isCritical)
{
    public BattleUnit Source { get; } = source;

    public BattleUnit Target { get; set; } = target;

    public SkillInstance? Skill { get; } = skill;

    public int ProposedAmount { get; set; } = proposedAmount;

    public bool IsCritical { get; } = isCritical;

    public bool SuppressHitEffects { get; set; }
}

internal sealed record BattleDamageTakenContext(
    BattleUnit Source,
    BattleUnit Target,
    SkillInstance? Skill,
    int IncomingAmount,
    int ActualAmount,
    bool IsCritical);

internal sealed class BattleDamageResolver(BattleEngine engine)
{
    public BattleDamageApplicationResult Apply(
        BattleState state,
        BattleUnit source,
        BattleUnit target,
        int amount,
        SkillInstance? skill = null,
        bool isCritical = false,
        bool runBeforeDamageApplied = true,
        bool runAfterDamageApplied = true,
        double lifestealRateBonus = 0d,
        HookTiming? eventTiming = null,
        string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var applicationContext = new BattleDamageApplicationContext(
            source,
            target,
            skill,
            amount,
            isCritical);
        if (runBeforeDamageApplied)
        {
            var application = engine.TriggerHooks(
                state,
                HookTiming.BeforeDamageApplied,
                target,
                context =>
                {
                    context.Source = source;
                    context.Target = target;
                    context.Skill = skill;
                    context.DamageAmount = amount;
                    context.IsCritical = isCritical;
                });
            applicationContext.ProposedAmount = Math.Max(0, application.DamageAmount ?? amount);
            applicationContext.Target = application.Target ?? target;
            applicationContext.SuppressHitEffects = application.SuppressHitEffects;
        }

        var actualAmount = applicationContext.Target.TakeDamage(applicationContext.ProposedAmount);
        BattleEngine.AddMessage(state, new BattleFact(
            BattleFactKind.Damaged,
            applicationContext.Target.Id,
            eventTiming,
            detail: detail ?? source.Id,
            damage: new BattleDamageEvent(actualAmount, isCritical, source.Id)));

        if (actualAmount > 0 && runAfterDamageApplied)
        {
            var takenContext = new BattleDamageTakenContext(
                source,
                applicationContext.Target,
                skill,
                applicationContext.ProposedAmount,
                actualAmount,
                isCritical);
            engine.TriggerHooks(state, HookTiming.OnDamageTaken, takenContext.Target, context =>
            {
                context.Source = takenContext.Source;
                context.Target = takenContext.Target;
                context.Skill = takenContext.Skill;
                context.DamageAmount = takenContext.ActualAmount;
                context.IsCritical = takenContext.IsCritical;
            });

            TryGainRageFromTakingDamage(state, takenContext.Source, takenContext.Target);

            if (!takenContext.Target.IsAlive)
            {
                engine.TriggerHooks(state, HookTiming.BeforeDefeated, takenContext.Target, context =>
                {
                    context.Source = takenContext.Source;
                    context.Target = takenContext.Target;
                    context.Skill = takenContext.Skill;
                    context.IncomingDamageAmount = takenContext.IncomingAmount;
                    context.DamageAmount = takenContext.ActualAmount;
                    context.IsCritical = takenContext.IsCritical;
                });
            }

            var lifestealRate = Math.Max(
                0d,
                takenContext.Source.GetStat(StatType.Lifesteal) + lifestealRateBonus);
            var dealt = engine.TriggerHooks(state, HookTiming.OnDamageDealt, takenContext.Source, context =>
            {
                context.Source = takenContext.Source;
                context.Target = takenContext.Target;
                context.Skill = takenContext.Skill;
                context.DamageAmount = takenContext.ActualAmount;
                context.IsCritical = takenContext.IsCritical;
                context.LifestealRate = lifestealRate;
            });

            ApplyLifesteal(state, takenContext, dealt.LifestealRate ?? lifestealRate);
        }

        return new BattleDamageApplicationResult(
            applicationContext.Target,
            actualAmount,
            applicationContext.SuppressHitEffects);
    }

    private void TryGainRageFromTakingDamage(BattleState state, BattleUnit source, BattleUnit target)
    {
        if (!state.AreEnemies(source, target))
        {
            return;
        }

        var chance = 0.5d + target.GetStat(StatType.Fuyuan) / 1000d;
        if (target.HasTrait(TraitId.Irascible))
        {
            chance += 0.15d;
        }

        if (!Probability.RollChance(engine.RandomService, chance))
        {
            return;
        }

        var amount = target.HasTrait(TraitId.DoubleDamageTakenRageGain) ? 2 : 1;
        BattleResourceResolver.AddRage(
            state,
            target,
            amount,
            HookTiming.OnDamageTaken,
            "damaged");
    }

    private void ApplyLifesteal(BattleState state, BattleDamageTakenContext context, double rate)
    {
        if (context.Skill is null ||
            !context.Source.IsAlive ||
            context.Source.Team == context.Target.Team)
        {
            return;
        }

        var requestedAmount = (int)Math.Floor(context.ActualAmount * Math.Max(0d, rate));
        if (requestedAmount <= 0)
        {
            return;
        }

        var actualAmount = engine.RecoveryResolver.Apply(
            state,
            context.Source,
            context.Source,
            BattleRecoveryKind.Hp,
            requestedAmount).ActualAmount;
        if (actualAmount > 0)
        {
            BattleEngine.AddMessage(state, new BattleFact(
                BattleFactKind.Lifesteal,
                context.Source.Id,
                HookTiming.OnDamageDealt,
                lifesteal: new BattleLifestealEvent(actualAmount)));
        }
    }
}
