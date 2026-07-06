using Game.Core.Model.Skills;

namespace Game.Core.Battle;

public sealed record BattleTurnCandidateGenerationOptions(
    bool AllowSkillCandidates = true,
    bool AllowRestCandidates = true,
    bool AllowMovement = true,
    Func<SkillInstance, bool>? SkillFilter = null)
{
    public static BattleTurnCandidateGenerationOptions Default { get; } = new();
}
