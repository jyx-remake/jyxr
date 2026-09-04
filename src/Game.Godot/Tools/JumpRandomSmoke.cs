using Game.Application.Mods;
using Game.Expressions;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check that jump_random resolves against the real xmjh story
/// repository (validates candidates, returns one of them).
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/JumpRandomSmoke.cs
/// </summary>
public partial class JumpRandomSmoke : SceneTree
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
			var result = await Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall("jump_random(['少林_伙房阿星1', '少林_伙房阿星2', '少林_伙房阿星3'])"));

			var picked = result.JumpTarget;
			if (picked != "少林_伙房阿星1" && picked != "少林_伙房阿星2" && picked != "少林_伙房阿星3")
			{
				throw new InvalidOperationException($"jump_random picked an unexpected target: '{picked}'.");
			}

			GD.Print($"JUMPRANDOM_SMOKE picked={picked}");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"JUMPRANDOM_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}
}
