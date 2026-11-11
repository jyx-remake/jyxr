using Game.Application;
using Game.Core.Model;
using Game.Core.Story;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("story")]
	private async ValueTask<StoryCommandResult> ExecuteStoryAsync(string storyId, CancellationToken cancellationToken)
	{
		var session = Game.Session;
		var state = session.State;
		var wasStoryPresentationActive = UIRoot.Instance.IsStoryPresentationActive;
		if (!wasStoryPresentationActive)
		{
			UIRoot.Instance.SetStoryPresentationActive(true);
		}

		try
		{
			var outcome = await Game.StoryService.ExecuteAsync(storyId, cancellationToken: cancellationToken);
			return outcome == StoryOutcome.Completed ? StoryCommandResult.None : StoryCommandResult.End(outcome);
		}
		finally
		{
			if (!wasStoryPresentationActive && ReferenceEquals(session, Game.Session) &&
				ReferenceEquals(state, session.State) && global::Godot.GodotObject.IsInstanceValid(UIRoot.Instance))
			{
				UIRoot.Instance.SetStoryPresentationActive(false);
			}
		}
	}

	[StoryCommand("map", "set_map", "tutorial")]
	private ValueTask ExecuteMapAsync(string mapId, params string[] locationIds)
	{
		if (locationIds.Length > 1)
		{
			throw new InvalidOperationException("Map command accepts at most one location id.");
		}

		if (locationIds.Length == 0)
		{
			World.Instance.EnterMap(mapId);
		}
		else
		{
			World.Instance.EnterMap(mapId, locationIds[0]);
		}

		return ValueTask.CompletedTask;
	}

	[StoryCommand("shop")]
	private ValueTask ExecuteShopAsync(string shopId, CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowShopPanelAsync(shopId, cancellationToken));

	[StoryCommand("chest", "xiangzi")]
	private ValueTask ExecuteChestAsync(CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowChestPanelAsync(cancellationToken));

	[StoryCommand("battle")]
	private async ValueTask<StoryCommandResult> ExecuteBattleAsync(string battleId, CancellationToken cancellationToken)
	{
		var selected = await UIRoot.Instance.ShowCombatantSelectPanelAsync(battleId, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		var isWin = await UIRoot.Instance.ShowBattleScreenAsync(
			new OrdinaryBattleRequest(battleId, selected.ToArray()),
			cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		if (!isWin)
		{
			return await Game.SessionFlowService.EndAsync(StoryOutcome.Defeated, this, cancellationToken);
		}
		return StoryCommandResult.None;
	}

	[StoryCommand("background")]
	private ValueTask ExecuteBackgroundAsync(string backgroundId)
	{
		World.Instance.SetBackground(backgroundId);
		return ValueTask.CompletedTask;
	}
}
