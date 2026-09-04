using Game.Application.Mods;
using Game.Core.Model;
using Game.Expressions;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check for give_gift: drives the story call, presses the first
/// backpack entry in the reused item panel, and asserts the wpxz story
/// variable records its 1-based index among the candidates.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/GiftSmoke.cs
/// </summary>
public partial class GiftSmoke : SceneTree
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

			var gift = Game.ContentRepository.GetItems().FirstOrDefault()
				?? throw new InvalidOperationException("Item repository is empty; cannot pick a gift.");
			Game.InventoryService.AddItem(gift, 1, notifyAcquisition: false);
			var firstId = gift.Id;

			var parser = new ExpressionParser();
			var callTask = Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall($"give_gift(['{firstId}', 'definitely-not-an-item-xyz'])")).AsTask();

			for (var frame = 0; frame < 30 && !IsPickPanelOpen(); frame++)
			{
				await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			}

			var box = FindFirstItemBox()
				?? throw new InvalidOperationException("Item pick panel did not open for give_gift.");
			box.EmitSignal(BaseButton.SignalName.Pressed);

			await callTask;

			if (!Game.State.Story.TryGetVariable("wpxz", out var value) || value.AsNumber("wpxz") != 1)
			{
				throw new InvalidOperationException("wpxz was not recorded as 1 after picking the first candidate.");
			}

			GD.Print($"GIFT_SMOKE picked={firstId} wpxz=1");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"GIFT_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private static bool IsPickPanelOpen() => FindFirstItemBox() is not null;

	private static TextureButton? FindFirstItemBox()
	{
		foreach (var descendant in UI.UIRoot.Instance.ModalLayer.FindChildren("*", "", true, false))
		{
			if (descendant.GetType().Name == "InventoryItemBox" && descendant is TextureButton box)
			{
				return box;
			}
		}

		return null;
	}
}
