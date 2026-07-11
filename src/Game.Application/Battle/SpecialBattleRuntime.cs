namespace Game.Application;

public sealed record CombatantSelectionRequest(
    string BattleId,
    IReadOnlySet<string> ForbiddenCharacterIds,
    string? Title = null);

public abstract record SpecialBattleRequest(
    string BattleId,
    IReadOnlyList<string> SelectedCharacterIds);

public sealed record OrdinaryBattleRequest(
    string BattleId,
    IReadOnlyList<string> SelectedCharacterIds)
    : SpecialBattleRequest(BattleId, SelectedCharacterIds);

public sealed record ArenaBattleRequest(
    string BattleId,
    IReadOnlyList<string> SelectedCharacterIds,
    int HardLevel)
    : SpecialBattleRequest(BattleId, SelectedCharacterIds);

public sealed record ZhenlongqijuBattleRequest(
    string BattleId,
    IReadOnlyList<string> SelectedCharacterIds,
    int Level)
    : SpecialBattleRequest(BattleId, SelectedCharacterIds);

public interface ISpecialBattleRuntimeHost
{
    ValueTask<IReadOnlyList<string>> SelectCombatantsAsync(
        CombatantSelectionRequest request,
        CancellationToken cancellationToken);

    ValueTask<bool> RunBattleAsync(
        SpecialBattleRequest request,
        CancellationToken cancellationToken);
}
