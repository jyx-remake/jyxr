using Game.Core.Affix;

namespace Game.Core.Battle.Talents;

/// <summary>
/// Deals source-side damage when the unit owning this hook has an ally with a
/// required talent. This mirrors legacy protective/retaliatory Lua branches.
/// </summary>
public sealed record AllyTalentSourceDamageBattleEffectParameters(
    [property: NotWhiteSpace] string AllyTalentId,
    [property: NonNegative] double SourceMpFactor,
    [property: Probability] double Chance = 1d);

internal sealed class AllyTalentSourceDamageBattleEffectHandler
    : CustomBattleEffectHandler<AllyTalentSourceDamageBattleEffectParameters, IHitConfirmedEffectContext>
{
    public override IReadOnlySet<HookTiming> SupportedTimings { get; } =
        new HashSet<HookTiming> { HookTiming.OnHitConfirmed };

    public override void Execute(
        IHitConfirmedEffectContext context,
        AllyTalentSourceDamageBattleEffectParameters parameters)
    {
        if (context is not BattleHookContext hookContext ||
            context.Source is null ||
            !Probability.RollChance(context.Random, parameters.Chance) ||
            !context.State.GetLivingUnits().Any(unit =>
                unit.Team == context.Unit.Team &&
                unit.Character.HasEffectiveTalent(parameters.AllyTalentId)))
        {
            return;
        }

        var amount = (int)Math.Ceiling(context.Source.Mp * parameters.SourceMpFactor);
        if (amount > 0)
        {
            hookContext.Damage(context.Source, amount, "三阳开泰·武当九阳");
        }
    }
}
