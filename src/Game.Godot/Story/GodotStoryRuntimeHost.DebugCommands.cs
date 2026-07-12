using Game.Application;
using Game.Core.Model;
using Game.Core.Story;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("debug_battle", "dbattle")]
	private async ValueTask ExecuteDebugBattleAsync(
		string battleId,
		CancellationToken cancellationToken)
	{
		var selectedCharacterIds = await UIRoot.Instance.ShowCombatantSelectPanelAsync(
			battleId,
			cancellationToken);
		await UIRoot.Instance.ShowBattleScreenAsync(
			new OrdinaryBattleRequest(battleId, selectedCharacterIds.ToArray()),
			cancellationToken);
	}
}
