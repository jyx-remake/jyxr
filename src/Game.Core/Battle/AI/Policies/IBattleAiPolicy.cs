namespace Game.Core.Battle;

public interface IBattleAiPolicy
{
    IReadOnlyList<BattleTurnCandidate> GenerateCandidates(
        BattleState state,
        BattleUnit unit,
        BattleTurnCandidateGenerator generator);
}
