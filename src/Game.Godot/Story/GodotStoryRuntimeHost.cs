using Game.Application;
using Game.Core.Model;
using Game.Core.Story;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost : IRuntimeHost, ISpecialBattleRuntimeHost, IApplicationRuntimeHost, IMiniGameRuntimeHost
{
	public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowDialogueAsync(dialogue.Speaker, dialogue.Text, cancellationToken));

	public async ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
	{
		var visibleIndex = await UIRoot.Instance.ShowChoicesAsync(
			choice.PromptSpeaker,
			choice.PromptText,
			choice.Options.Select(static option => option.Text).ToArray(),
			choice.Style,
			cancellationToken);
		return choice.Options[visibleIndex].Index;
	}

	public async ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(battle);
		var selectedCharacterIds = await UIRoot.Instance.ShowCombatantSelectPanelAsync(battle.BattleId, cancellationToken);
		var isWin = await UIRoot.Instance.ShowBattleScreenAsync(
			new OrdinaryBattleRequest(battle.BattleId, selectedCharacterIds.ToArray()),
			cancellationToken);
		return isWin ? BattleOutcome.Win : BattleOutcome.Lose;
	}

	public async ValueTask<InventoryEntry?> SelectRefinementEquipmentAsync(
		IReadOnlyList<InventoryEntry> entries,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entries);
		return await UIRoot.Instance.ShowRefinementEquipmentSelectionPanelAsync(entries, cancellationToken);
	}

	public async ValueTask<IReadOnlyList<string>> SelectCombatantsAsync(
		CombatantSelectionRequest request,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		return await UIRoot.Instance.ShowCombatantSelectPanelAsync(
			request.BattleId,
			request.ForbiddenCharacterIds,
			cancellationToken);
	}

	public async ValueTask<bool> RunBattleAsync(
		SpecialBattleRequest request,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		return await UIRoot.Instance.ShowBattleScreenAsync(request, cancellationToken);
	}

	public ValueTask<int> RunLightnessTrainingAsync(CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowLightnessTrainingScreenAsync(cancellationToken));

	public ValueTask<(int Score, IReadOnlyDictionary<string, int> ItemCounts)> RunStrengthTrainingAsync(
		IReadOnlyList<string> itemIds,
		CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowStrengthTrainingScreenAsync(itemIds, cancellationToken));

	public ValueTask PlayEffectAsync(string effectId, CancellationToken cancellationToken) => ExecuteEffectAsync(effectId);

	public ValueTask GameOverAsync(CancellationToken cancellationToken) => ExecuteGameOverAsync();
}
