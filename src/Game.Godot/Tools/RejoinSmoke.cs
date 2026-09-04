using Game.Application.Mods;
using Game.Core.Model;
using Game.Expressions;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless repro for the in-game rejoin_menu failure: drives the exact
/// story call through the dispatcher, auto-closes the recall panel, and
/// reports the full exception (if any).
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/RejoinSmoke.cs
/// </summary>
public partial class RejoinSmoke : SceneTree
{
	public override void _Initialize() => CallDeferred(nameof(RunSmoke));

	private async void RunSmoke()
	{
		try
		{
			var projectRoot = ProjectSettings.GlobalizePath("res://");
			var dataRoot = ProjectDataRoot.FromPath(projectRoot);
			var mods = new ModRegistry(dataRoot).DiscoverMods();
			var loadout = new ModLoadoutResolver(mods).Resolve("xmjh", []);

			GameRuntimeBootstrap.Initialize(loadout, this);

			// Ensure at least one kicked companion exists so the panel
			// exercises the card path rather than the empty path.
			var companionId = Game.ContentRepository.GetCharacters()
				.Select(definition => definition.Id)
				.FirstOrDefault(id => !string.Equals(id, Party.HeroCharacterId, StringComparison.Ordinal));
			if (!string.IsNullOrWhiteSpace(companionId))
			{
				Game.PartyService.Join(companionId);
				Game.PartyService.Kick(companionId);
			}

			var parser = new ExpressionParser();
			var callTask = Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall("rejoin_menu('神秘少女')")).AsTask();

			for (var frame = 0; frame < 10; frame++)
			{
				await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			}

			var recallPanel = FindRecallPanel();
			GD.Print($"REJOIN_SMOKE panel_open={(recallPanel is not null)} kicked={CountKicked()}");

			if (recallPanel is not null)
			{
				recallPanel.QueueFree();
			}

			await callTask;
			GD.Print("REJOIN_SMOKE rejoin_menu=ok");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"REJOIN_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private static Node? FindRecallPanel()
	{
		foreach (var descendant in UI.UIRoot.Instance.FindChildren("*", "", true, false))
		{
			if (descendant.GetType().Name == "RecallPanel")
			{
				return descendant;
			}
		}

		return null;
	}

	private static int CountKicked()
	{
		var count = 0;
		foreach (var character in Game.State.Party.GetAllCharacters())
		{
			if (character.LeaveState == CharacterLeaveState.Kicked)
			{
				count++;
			}
		}

		return count;
	}
}
