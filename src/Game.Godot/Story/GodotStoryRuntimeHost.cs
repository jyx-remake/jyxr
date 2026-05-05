using Game.Core.Story;
using Game.Core.Model;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Story;

public sealed class GodotStoryRuntimeHost : IRuntimeHost
{
	private readonly StoryCommandBinder _binder;

	public GodotStoryRuntimeHost()
	{
		_binder = new StoryCommandBinder(this);
	}

	public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowDialogueAsync(dialogue.Speaker, dialogue.Text, cancellationToken));

	public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
		ValueTask.FromException<ExprValue>(new InvalidOperationException($"Story variable '{name}' is not provided by Godot runtime host."));

	public ValueTask<bool> EvaluatePredicateAsync(
		string name,
		IReadOnlyList<ExprValue> args,
		CancellationToken cancellationToken) =>
		ValueTask.FromException<bool>(new InvalidOperationException($"Story predicate '{name}' is not provided by Godot runtime host."));

	public ValueTask ExecuteCommandAsync(
		string name,
		IReadOnlyList<ExprValue> args,
		CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentNullException.ThrowIfNull(args);

		if (_binder.TryExecute(name, args, cancellationToken, out var result))
		{
			return result;
		}

		return ValueTask.FromException(new InvalidOperationException($"Unsupported Godot story command '{name}'."));
	}

	public async ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
		=> await UIRoot.Instance.ShowChoicesAsync(
			choice.PromptSpeaker,
			choice.PromptText,
			choice.Options.Select(static option => option.Text).ToArray(),
			cancellationToken);

	public async ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(battle);
		var selectedCharacterIds = await UIRoot.Instance.ShowCombatantSelectPanelAsync(battle.BattleId, cancellationToken);
		var isWin = await UIRoot.Instance.ShowBattleScreenAsync(
			battle.BattleId,
			selectedCharacterIds,
			cancellationToken);
		return isWin ? BattleOutcome.Win : BattleOutcome.Lose;
	}

	[StoryCommand("map", "set_map", "tutorial")]
	private ValueTask ExecuteMapAsync(string mapId)
	{
		GetWorld().EnterMap(mapId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("shop")]
	private async ValueTask ExecuteShopAsync(string shopId, CancellationToken cancellationToken)
	{
		var panel = UIRoot.Instance.ShowShopPanel(shopId);
		using var registration = cancellationToken.Register(() =>
		{
			if (GodotObject.IsInstanceValid(panel))
			{
				panel.QueueFree();
			}
		});
		await panel.ToSignal(panel, Node.SignalName.TreeExiting);
	}

	[StoryCommand("music")]
	private ValueTask ExecuteMusicAsync(params string[] trackIds)
	{
		if (trackIds.Length == 0)
		{
			throw new InvalidOperationException("Command 'music' requires at least one argument.");
		}

		if (trackIds.Length == 1)
		{
			Game.Audio.PlayBgm(trackIds[0]);
			return ValueTask.CompletedTask;
		}

		Game.Audio.PlayBgm(trackIds);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("effect")]
	private ValueTask ExecuteEffectAsync(string effectId)
	{
		Game.Audio.PlaySfx(effectId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("background")]
	private ValueTask ExecuteBackgroundAsync(string backgroundId)
	{
		GetWorld().SetBackground(backgroundId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("suggest")]
	private ValueTask ExecuteSuggestAsync(string text, CancellationToken cancellationToken)
	{
		return new ValueTask(UIRoot.Instance.ShowSuggestionAsync(text, cancellationToken));
	}

	[StoryCommand("toast")]
	private ValueTask ExecuteToastAsync(string mode)
	{
		UIRoot.Instance.SetToastSuppressed(ParseToastSuppressed(mode));
		return ValueTask.CompletedTask;
	}

	[StoryCommand("select_menpai", "select_sect")]
	private async ValueTask ExecuteSelectSectAsync(CancellationToken cancellationToken)
	{
		var sect = await UIRoot.Instance.ShowSelectSectScreenAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(sect.StoryId))
		{
			throw new InvalidOperationException($"Sect '{sect.Id}' does not define an entry story.");
		}

		await foreach (var _ in Game.StoryService.RunAsync(sect.StoryId, cancellationToken))
		{
		}
	}

	[StoryCommand("input_name")]
	private async ValueTask ExecuteInputNameAsync(
		string characterId,
		string defaultName = "",
		CancellationToken cancellationToken = default)
	{
		var name = await UIRoot.Instance.ShowInputNamePanelAsync(characterId, defaultName, cancellationToken);
		Game.CharacterService.RenameCharacter(characterId, name);
	}

	[StoryCommand("select_head")]
	private async ValueTask ExecuteSelectHeadAsync(string characterId, CancellationToken cancellationToken)
	{
		var head = await UIRoot.Instance.ShowSelectHeadPanelAsync(cancellationToken);
		Game.CharacterService.SetCharacterPortrait(characterId, head);
	}

	[StoryCommand("roll_stats")]
	private ValueTask ExecuteRollStatsAsync(CancellationToken cancellationToken) =>
		new(UIRoot.Instance.ShowRollStatsPanelAsync("主角", cancellationToken));

	[StoryCommand("shake")]
	private ValueTask ExecuteShakeAsync(double amplitude = 12d, double duration = 0.22d)
	{
		GetWorld().PlayScreenShake((float)amplitude, duration);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("head")]
	private ValueTask ExecuteHeadAsync(string portraitId)
	{
		Game.CharacterService.SetCharacterPortrait(Party.HeroCharacterId, portraitId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("animation")]
	private ValueTask ExecuteAnimationAsync(string characterId, string modelId)
	{
		Game.CharacterService.SetCharacterModel(characterId, modelId);
		return ValueTask.CompletedTask;
	}

	[StoryCommand("mainmenu")]
	private ValueTask ExecuteMainMenuAsync()
	{
		GameFlow.ReturnToMainMenu();
		return ValueTask.CompletedTask;
	}

	[StoryCommand("restart")]
	private async ValueTask ExecuteRestartAsync(string mode = "restart", CancellationToken cancellationToken = default)
	{
		if (!string.Equals(mode, "restart", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"Unsupported restart mode '{mode}'.");
		}

		await GameFlow.StartNewGameAsync(cancellationToken);
	}

	[StoryCommand("nextzhoumu")]
	private ValueTask ExecuteNextZhoumuAsync(CancellationToken cancellationToken) =>
		new(GameFlow.StartNextRoundAsync(cancellationToken));

	[StoryCommand("gameover")]
	private ValueTask ExecuteGameOverAsync()
	{
		Game.ProfileService.AddDeaths();
		UIRoot.Instance.ShowGameOverScreen();
		return ValueTask.CompletedTask;
	}

	private static bool ParseToastSuppressed(string mode)
	{
		return mode.Trim() switch
		{
			"off" => true,
			"on" => false,
			_ => throw new InvalidOperationException($"Unsupported toast mode '{mode}'. Use 'on' or 'off'."),
		};
	}

	private static World GetWorld() => GetRequiredNode<World>("/root/World");

	private static T GetRequiredNode<T>(string path) where T : Node
	{
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			throw new InvalidOperationException("Godot scene tree is not available.");
		}

		var node = tree.Root.GetNodeOrNull<T>(path);
		if (node is null)
		{
			throw new InvalidOperationException($"Required Godot node is missing: {path}.");
		}

		return node;
	}
}
