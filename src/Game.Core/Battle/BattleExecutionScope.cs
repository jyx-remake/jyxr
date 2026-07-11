namespace Game.Core.Battle;

public sealed record BattleExecutionScope(
    long Sequence,
    long? ParentSequence,
    int Depth,
    string RuleSource);
