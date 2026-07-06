namespace Game.Core.Battle;

internal sealed class RestOnlyBattleAiPolicy : IBattleAiPolicy
{
    public IReadOnlyList<BattleTurnCandidate> GenerateCandidates(
        BattleState state,
        BattleUnit unit,
        BattleTurnCandidateGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        return generator.Generate(
            state,
            unit.Id,
            new BattleTurnCandidateGenerationOptions(
                AllowSkillCandidates: false,
                AllowRestCandidates: true,
                AllowMovement: false));
    }
}
