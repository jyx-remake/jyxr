using Game.Core.Abstractions;
using Game.Core.Model;
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

    public bool IsCellAvailable(GridPosition position, BattleUnit movingUnit) =>
        state.Grid.IsWalkable(position) && !state.IsOccupied(position, movingUnit.Id);

    public bool TryRelocate(BattleUnit target, GridPosition destination) =>
        engine.TryRelocateByEffect(state, target, destination);

    public int ApplyHpRecovery(BattleUnit target, int amount) =>
        engine.ApplyDirectHpRecovery(state, Source, target, amount);

    public int ApplyDirectDamage(BattleUnit target, int amount, string? detail = null) =>
        engine.ApplyDirectDamage(state, Source, target, amount, detail: detail);
}
