namespace Game.Core.Battle;

internal sealed class AttackOnlyBattleAiPolicy : IBattleAiPolicy
{
    public IReadOnlyList<BattleTurnCandidate> GenerateCandidates(
        BattleState state,
        BattleUnit unit,
        BattleTurnCandidateGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        var attackCandidates = generator.Generate(
            state,
            unit.Id,
            new BattleTurnCandidateGenerationOptions(
                AllowSkillCandidates: true,
                AllowRestCandidates: false,
                AllowMovement: true));
        if (attackCandidates.Count > 0)
        {
            return attackCandidates;
        }

        return generator.Generate(
            state,
            unit.Id,
            new BattleTurnCandidateGenerationOptions(
                AllowSkillCandidates: false,
                AllowRestCandidates: true,
                AllowMovement: true));
    }
}
