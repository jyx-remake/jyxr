using Game.Application;
using Game.Core.Model;
using Game.Core.Story;
using Game.Expressions;
using Game.Godot.Map;
using Game.Godot.UI;

namespace Game.Godot.Story;

public sealed partial class GodotStoryRuntimeHost
{
	[StoryCommand("story")]
	private async ValueTask<StoryCommandResult> ExecuteStoryAsync(string storyId, CancellationToken cancellationToken)
	{
		var wasStoryPresentationActive = UIRoot.Instance.IsStoryPresentationActive;
		if (!wasStoryPresentationActive)
		{
			UIRoot.Instance.SetStoryPresentationActive(true);
		}

		try
		{
			var executionState = Game.State;
			var result = await StoryRunHelper.RunAsync(storyId, cancellationToken);
			if (!result.WasHandled &&
				ReferenceEquals(executionState, Game.State))
			{
				throw new InvalidOperationException($"Story command '{storyId}' did not complete a segment.");
			}

			return result.Terminated
				? StoryCommandResult.Terminate
				: StoryCommandResult.None;
		}
		finally
		{
			if (!wasStoryPresentationActive && global::Godot.GodotObject.IsInstanceValid(UIRoot.Instance))
			{
				UIRoot.Instance.SetStoryPresentationActive(false);
			}
		}
	}

	[StoryCommand("story_by_hero_name")]
	private ValueTask<StoryCommandResult> ExecuteStoryByHeroNameAsync(
		string prefix = "豪名_",
		CancellationToken cancellationToken = default)
	{
		var hero = Game.State.Party.GetMember(Party.HeroCharacterId);
		return ExecuteStoryAsync($"{prefix}{hero.Name}", cancellationToken);
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

	[StoryCommand("set_location")]
	private ValueTask ExecuteSetLocationAsync(string mapId, string locationId)
	{
		Game.MapService.SetLocation(mapId, locationId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("shop")]
	private ValueTask ExecuteShopAsync(string shopId, CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowShopPanelAsync(shopId, cancellationToken));

	[StoryCommand("chest", "xiangzi")]
	private ValueTask ExecuteChestAsync(CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowChestPanelAsync(cancellationToken));

	[StoryCommand("rejoin_menu")]
	private ValueTask ExecuteRejoinMenuAsync(
		string defaultCharacterId = "神秘少女",
		CancellationToken cancellationToken = default) =>
		new(UIRoot.Instance.ShowRecallPanelAsync(defaultCharacterId, cancellationToken));

	[StoryCommand("give_gift")]
	private async ValueTask<StoryCommandResult> ExecuteGiveGiftAsync(
		IReadOnlyList<string> itemIds,
		CancellationToken cancellationToken)
	{
		if (itemIds.Count == 0)
		{
			throw new ArgumentException("give_gift requires at least one candidate item.", nameof(itemIds));
		}

		var picked = await UIRoot.Instance.ShowItemPickPanelAsync("请选择需要出示的物品", cancellationToken);
		var index = GiftResolutionService.ResolveGiftIndex(picked?.Definition, [.. itemIds]);
		Game.Session.State.Story.SetVariable(
			GiftResolutionService.GiftVariableName,
			ExpressionValue.FromNumber(index));
		Game.Session.Events.Publish(new StoryStateChangedEvent());
		return StoryCommandResult.None;
	}

	[StoryCommand("battle")]
	private async ValueTask ExecuteBattleAsync(string packedBattleId, CancellationToken cancellationToken)
	{
		var (battleId, totalBattles, battleLevel) = ParseLegacyBattleReference(packedBattleId);
		var selected = await UIRoot.Instance.ShowCombatantSelectPanelAsync(battleId, cancellationToken);
		for (var index = 0; index < totalBattles; index++)
		{
			var isWin = await UIRoot.Instance.ShowBattleScreenAsync(
				new OrdinaryBattleRequest(battleId, selected.ToArray(), battleLevel),
				cancellationToken);
			if (!isWin)
			{
				GameFlow.GameOver();
				return;
			}
		}
	}

	private static (string BattleId, int TotalBattles, int BattleLevel) ParseLegacyBattleReference(string packedBattleId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(packedBattleId);
		var parts = packedBattleId.Split('#');
		var battleId = parts[0].Trim();
		if (battleId.Length == 0)
		{
			throw new InvalidOperationException("Battle command requires a battle id.");
		}
		if (parts.Length > 3)
		{
			throw new InvalidOperationException("Battle command accepts at most totalBattles and battleLevel legacy parameters.");
		}

		var totalBattles = 1;
		if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
		{
			if (!int.TryParse(parts[1].Trim(), out var parsedTotalBattles))
			{
				throw new InvalidOperationException($"Invalid legacy battle count: '{parts[1]}'.");
			}
			totalBattles = Math.Max(1, parsedTotalBattles);
		}

		var battleLevel = 0;
		if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
		{
			if (!int.TryParse(parts[2].Trim(), out var parsedBattleLevel))
			{
				throw new InvalidOperationException($"Invalid legacy battle level: '{parts[2]}'.");
			}
			battleLevel = parsedBattleLevel is > 0 and <= 1000 ? parsedBattleLevel : 0;
		}

		return (battleId, totalBattles, battleLevel);
	}

	[StoryCommand("background")]
	private ValueTask ExecuteBackgroundAsync(string backgroundId, ExpressionValue legacyExtra = default)
	{
		// Legacy BACKGROUND: a lone argument shows the backdrop at the ambient
		// time-of-day opacity; any extra argument requests full alpha. Keeping
		// this on the same rule as `fadein` avoids a brightness pop when a
		// story switches between the two commands.
		var alpha = legacyExtra == default
			? MapTimeLighting.GetAmbientOpacity(Game.State.Clock.TimeSlot)
			: 1f;
		World.Instance.SetBackground(ResolveBackdropCandidate(backgroundId), alpha);
		return ValueTask.CompletedTask;
	}
}
