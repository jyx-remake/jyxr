using Game.Application.Mods;
using Game.Core.Model;
using Game.Core.Persistence;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check for journal bbcode: a journal entry carrying legacy
/// color markup must land in a bbcode-enabled RichTextLabel instead of a
/// plain Label that would print the tags literally.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/JournalSmoke.cs
/// </summary>
public partial class JournalSmoke : SceneTree
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

			var entryScene = GD.Load<PackedScene>("res://scenes/ui/journal/journal_entry.tscn")
				?? throw new InvalidOperationException("journal_entry.tscn could not be loaded.");
			var entry = entryScene.Instantiate();
			if (entry.GetType().Name != "JournalEntryLabel" || entry is not RichTextLabel label)
			{
				throw new InvalidOperationException("Journal entry root must be a bbcode RichTextLabel.");
			}

			Root.AddChild(entry);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			((UI.JournalEntryLabel)entry).Setup(new JournalEntry(
				new ClockRecord(1, 1, 1, TimeSlot.Wu),
				"原来他是[color=red]华山[/color]的高徒"));

			Check(label.BbcodeEnabled, "journal entry must parse bbcode");
			Check(label.Text.Contains("[color=red]华山[/color]", StringComparison.Ordinal), "journal text preserved");

			GD.Print("JOURNAL_SMOKE bbcode=ok");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"JOURNAL_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private static void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException($"Journal assertion failed: {message}.");
		}
	}
}
