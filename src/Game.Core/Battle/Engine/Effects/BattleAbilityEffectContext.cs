using Game.Core.Abstractions;
using Game.Core.Model.Skills;

namespace Game.Core.Battle;

internal sealed class BattleAbilityEffectContext(
    BattleEngine engine,
    BattleState state,
    BattleUnit source,
    IReadOnlyList<BattleUnit> targets,
    SkillInstance skill,
    IRandomService random) : IBattleAbilityEffectContext
{
    public BattleUnit Source { get; } = source;

    public SkillInstance Skill { get; } = skill;

    public IReadOnlyList<BattleUnit> Targets { get; } = targets;

    public IRandomService Random { get; } = random;

    public BattleExecutionScope? ExecutionScope => state.CurrentExecutionScope;

    public int ApplyDirectDamage(BattleUnit target, int amount, string? detail = null) =>
        engine.ApplyDirectDamage(state, Source, target, amount, detail: detail);
}
