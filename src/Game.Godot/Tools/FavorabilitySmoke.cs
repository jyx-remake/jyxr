using Game.Application.Mods;
using Game.Expressions;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check for show_favorability: drives the converted call through
/// the dispatcher, asserts the suggestion panel shows the live favorability
/// value, then acknowledges it.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/FavorabilitySmoke.cs
/// </summary>
public partial class FavorabilitySmoke : SceneTree
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

			var parser = new ExpressionParser();
			var callTask = Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall("show_favorability('日月贡献')")).AsTask();

			Node? hintBox = null;
			for (var frame = 0; frame < 30 && hintBox is null; frame++)
			{
				await ToSignal(this, SceneTree.SignalName.ProcessFrame);
				hintBox = FindHintBox();
			}

			if (hintBox is null)
			{
				throw new InvalidOperationException("Suggestion panel did not open for show_favorability.");
			}

			var expected = $"日月贡献：{Game.State.Adventure.GetFavorability("日月贡献")}";
			var shown = hintBox.GetNode<RichTextLabel>("%ContentLabel").Text;
			if (!shown.Contains(expected, StringComparison.Ordinal))
			{
				throw new InvalidOperationException($"Suggestion shows '{shown}', expected '{expected}'.");
			}

			hintBox.GetNode<BaseButton>("%AckButton").EmitSignal(BaseButton.SignalName.Pressed);
			await callTask;

			GD.Print($"FAVORABILITY_SMOKE shown={shown}");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"FAVORABILITY_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private static Node? FindHintBox()
	{
		foreach (var descendant in UI.UIRoot.Instance.FindChildren("*", "", true, false))
		{
			if (descendant.GetType().Name == "HintBox" && (descendant as Control)?.Visible == true)
			{
				return descendant;
			}
		}

		return null;
	}
}
