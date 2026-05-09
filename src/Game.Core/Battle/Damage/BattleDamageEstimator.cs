using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed class BattleDamageEstimator
{
    private readonly BattleDamageCalculator _damageCalculator;

    public BattleDamageEstimator(BattleDamageCalculator? damageCalculator = null)
    {
        _damageCalculator = damageCalculator ?? new BattleDamageCalculator();
    }

    public BattleDamageEstimate Estimate(BattleUnit source, BattleUnit target, SkillInstance skill)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(skill);

        var result = _damageCalculator.EstimateSkillDamage(new BattleDamageContext(source, target, skill));
        var multiplier = BattleDamageRules.GetSkillDamageMultiplier(source, target);
        var damage = (int)Math.Floor(result.Amount * multiplier);
        return new BattleDamageEstimate(
            target,
            damage,
            IsEnemy: source.Team != target.Team,
            IsKill: damage >= target.Hp);
    }
}

public sealed record BattleDamageEstimate(
    BattleUnit Target,
    int Damage,
    bool IsEnemy,
    bool IsKill);
