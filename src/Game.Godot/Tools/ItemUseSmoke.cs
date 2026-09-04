using Game.Application.Mods;
using Game.Core.Definitions;
using Game.Core.Model;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless check for converted item behaviors: rolekey-pinned items
/// auto-target the hero (no selection UI), and equipped gear grants and
/// revokes its carried skills.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/ItemUseSmoke.cs
/// </summary>
public partial class ItemUseSmoke : SceneTree
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

			var heroId = Party.HeroCharacterId;

			var mask = Game.ContentRepository.GetItem("雄霸面具");
			Game.InventoryService.AddItem(mask, 1, notifyAcquisition: false);
			var maskEntry = Game.State.Inventory.GetStack(mask);
			var autoTarget = Game.ItemUseService.ResolveAutoTargetCharacterId(maskEntry);
			Check(autoTarget == heroId, $"mask should auto-target hero, got '{autoTarget}'");
			var maskResult = await Game.ItemUseService.UseAsync(maskEntry, autoTarget!);
			Check(maskResult.Success, $"mask use should succeed: {maskResult.Message}");
			GD.Print($"ITEMUSE_SMOKE rolekey=ok message={maskResult.Message}");

			var tome = Game.ContentRepository.GetItem("武道天书");
			Game.InventoryService.AddItem(tome, 1, notifyAcquisition: false);
			Game.InventoryService.EquipFromStack(heroId, (EquipmentDefinition)tome);
			var hero = Game.State.Party.GetMember(heroId);
			Check(hero.ExternalSkills.Any(skill => skill.Id == "百家剑法"), "equipped tome should grant skill");
			Game.InventoryService.UnequipToInventory(hero, EquipmentSlotType.Accessory);
			Check(hero.ExternalSkills.All(skill => skill.Id != "百家剑法"), "unequipped tome should revoke skill");
			GD.Print("ITEMUSE_SMOKE equipment_skills=ok");

			// Granted skills render in the skill tab without a toggle, and
			// pressing the box must not open the detail panel.
			Game.InventoryService.EquipFromStack(heroId, (EquipmentDefinition)tome);
			var skillTabScene = GD.Load<PackedScene>("res://scenes/ui/skill_tab/skill_tab.tscn")
				?? throw new InvalidOperationException("skill_tab.tscn could not be loaded.");
			var skillTab = (UI.SkillTab)skillTabScene.Instantiate();
			Root.AddChild(skillTab);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			skillTab.Setup(Game.State.Party.GetMember(heroId));
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			var grantedBox = FindSkillBox(skillTab, "百家剑法")
				?? throw new InvalidOperationException("Granted skill box was not listed.");
			Check(!grantedBox.GetNode<TextureButton>("%ActiveButton").Visible, "granted skill must hide its toggle");
			grantedBox.EmitSignal("pressed");
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			Check(FindDetailPanel() is null, "granted skill press must not open the detail panel");
			GD.Print("ITEMUSE_SMOKE granted_skill_ui=ok");

			GD.Print("ITEMUSE_SMOKE ok");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"ITEMUSE_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private static void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException($"Item use assertion failed: {message}.");
		}
	}

	private static Node? FindSkillBox(UI.SkillTab tab, string skillName)
	{
		foreach (var descendant in tab.FindChildren("*", "", true, false))
		{
			if (descendant.GetType().Name == "SkillBox" &&
				descendant.GetNode<Label>("%NameLabel").Text == skillName)
			{
				return descendant;
			}
		}

		return null;
	}

	private static Node? FindDetailPanel()
	{
		foreach (var descendant in UI.UIRoot.Instance.FindChildren("*", "", true, false))
		{
			if (descendant.GetType().Name == "DetailPanel")
			{
				return descendant;
			}
		}

		return null;
	}
}
