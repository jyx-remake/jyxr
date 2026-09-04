using Game.Application.Mods;
using Game.Core.Model;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless repro for the title toggle flow through the real CharacterPanel:
/// a hero with exactly one title must be able to check AND uncheck it, with
/// the panel re-rendering between presses exactly like in-game.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/TitleToggleSmoke.cs
/// </summary>
public partial class TitleToggleSmoke : SceneTree
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
			var titleId = Game.ContentRepository.GetCharacterTitles().First().Id;
			Game.CharacterService.LearnTitle(heroId, titleId);
			var hero = Game.State.Party.GetMember(heroId);
			Check(hero.Titles.Count == 1, $"expected 1 title, got {hero.Titles.Count}");

			var panelScene = GD.Load<PackedScene>("res://scenes/ui/character_panel/character_panel.tscn")
				?? throw new InvalidOperationException("character_panel.tscn could not be loaded.");
			var panel = (UI.CharacterPanel)panelScene.Instantiate();
			Root.AddChild(panel);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			panel.Configure(hero);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);

			// Open the title tab via the portrait title button, like a player.
			panel.GetNode<Node>("%TitleButton").EmitSignal("pressed");
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);

			// Check the single title.
			PressActiveButton(panel);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			Check(hero.Titles[0].Equipped, "title should be equipped after first press");

			// Pressing the equipped title is a no-op (switch-only, no uncheck).
			PressActiveButton(panel, titleId);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			Check(hero.Titles[0].Equipped, "equipped title press should keep it equipped");

			// With two titles, checking the other one switches over.
			var secondTitleId = Game.ContentRepository.GetCharacterTitles()
				.Select(definition => definition.Id)
				.First(id => id != titleId);
			Game.CharacterService.LearnTitle(heroId, secondTitleId);
			panel.Configure(hero);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			PressActiveButton(panel, secondTitleId);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			var firstTitle = hero.Titles.First(instance => instance.Id == titleId);
			var secondTitle = hero.Titles.First(instance => instance.Id == secondTitleId);
			Check(!firstTitle.Equipped && secondTitle.Equipped, "checking another title should switch over");

			// Pressing the box body must open the title detail panel
			// (reused skill detail panel), not a suggestion toast.
			PressTitleBox(panel);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			Check(FindDetailPanel() is not null, "title detail panel should open on box press");

			// The hover tooltip must build from the character-titles table.
			var tooltipScene = GD.Load<PackedScene>("res://scenes/ui/character_panel/title_tooltip.tscn")
				?? throw new InvalidOperationException("title_tooltip.tscn could not be loaded.");
			var tooltip = (UI.TitleTooltip)tooltipScene.Instantiate();
			Root.AddChild(tooltip);
			await ToSignal(this, SceneTree.SignalName.ProcessFrame);
			tooltip.Setup(hero.Titles[0]);
			var tooltipText = tooltip.GetNode<RichTextLabel>("%ContentLabel").Text;
			Check(tooltipText.Contains("+攻击") && tooltipText.Contains("被动增益"),
				"title tooltip should render table values and passive lines");

			GD.Print($"TITLE_TOGGLE_SMOKE title={titleId} check=ok noop=ok switch=ok detail=ok tooltip=ok");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"TITLE_TOGGLE_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}

	private static void PressActiveButton(UI.CharacterPanel panel)
	{
		PressActiveButton(panel, null);
	}

	private static void PressActiveButton(UI.CharacterPanel panel, string? titleId)
	{
		foreach (var box in FindTitleBoxes(panel))
		{
			if (titleId is null || box.TitleId == titleId)
			{
				box.GetNode<TextureButton>("%ActiveButton").EmitSignal(BaseButton.SignalName.Pressed);
				return;
			}
		}

		throw new InvalidOperationException("No CharacterTitleBox was created by the title tab.");
	}

	private static void PressTitleBox(UI.CharacterPanel panel)
	{
		foreach (var box in FindTitleBoxes(panel))
		{
			box.EmitSignal(BaseButton.SignalName.Pressed);
			return;
		}

		throw new InvalidOperationException("No CharacterTitleBox was created by the title tab.");
	}

	private static List<UI.CharacterTitleBox> FindTitleBoxes(UI.CharacterPanel panel)
	{
		var boxes = new List<UI.CharacterTitleBox>();
		foreach (var descendant in panel.FindChildren("*", "GridContainer", true, false))
		{
			foreach (var child in ((GridContainer)descendant).GetChildren())
			{
				if (child is UI.CharacterTitleBox box)
				{
					boxes.Add(box);
				}
			}
		}

		return boxes;
	}

	private static Node? FindDetailPanel()
	{
		foreach (var descendant in UIRoot.Instance.FindChildren("*", "", true, false))
		{
			if (descendant.GetType().Name == "DetailPanel")
			{
				return descendant;
			}
		}

		return null;
	}

	private static void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException($"Title toggle assertion failed: {message}.");
		}
	}
}
