using Game.Application.Mods;
using Game.Core.Story;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.Tools;

/// <summary>
/// Headless integration smoke test for the real launcher loadout/bootstrap path.
/// Invoked with: godot --headless --path . -s res://src/Game.Godot/Tools/XmjhBootstrapSmoke.cs
/// </summary>
public partial class XmjhBootstrapSmoke : SceneTree
{
	public override void _Initialize() => CallDeferred(nameof(RunSmoke));

	private void RunSmoke()
	{
		try
		{
			var projectRoot = ProjectSettings.GlobalizePath("res://");
			var dataRoot = ProjectDataRoot.FromPath(projectRoot);
			var mods = new ModRegistry(dataRoot).DiscoverMods();
			var loadout = new ModLoadoutResolver(mods).Resolve("xmjh", []);

			GameRuntimeBootstrap.Initialize(loadout, this);

			var combatantAnimation = AssetResolver.LoadCombatantAnimation("baihu");
			var skillAnimation = AssetResolver.LoadSkillAnimation("jn1");
			var portrait = AssetResolver.LoadTexture("头像.红衣女");
			var legacyHeadItem = AssetResolver.LoadTexture("头像.斧头");
			var item = AssetResolver.LoadTexture("物品.小还丹");
			var map = AssetResolver.LoadTexture("地图.桃花源");
			var nativeTown = AssetResolver.LoadTexture("town.native.na1");
			var music = AssetResolver.LoadAudio("音乐.秦川夏");
			var video = AssetResolver.LoadVideo("xianjing");
			var mainMenuBackground = AssetResolver.LoadTexture(Game.Config.MainMenuBackground);
			var mainMenuMusic = AssetResolver.LoadAudio(Game.Config.MainMenuMusic);
			var mainMenu = GD.Load<PackedScene>(GameFlow.MainMenuScenePath);
			var mapMarker = GD.Load<PackedScene>("res://scenes/map/map_entity_slot.tscn");
			var mapEntityBox = GD.Load<PackedScene>("res://scenes/map/map_entity_box.tscn");
			var dialoguePanel = GD.Load<PackedScene>("res://scenes/ui/story/story_dialogue_panel.tscn");
			var openingStory = Game.Config.InitialStorySegmentId;
			var quiz = Game.ContentRepository.GetStorySegment(openingStory).Segment;
			var quizChoices = quiz.Steps.OfType<ChoiceStep>().ToArray();
			if (combatantAnimation is null || skillAnimation is null || portrait is null ||
				legacyHeadItem is null || item is null || nativeTown is null ||
				map is null || music is null || video is null || mainMenuBackground is null ||
				mainMenuMusic is null || mainMenu is null || mapMarker is null ||
				mapEntityBox is null || dialoguePanel is null ||
				!Game.ContentRepository.TryGetStorySegment(openingStory, out _) ||
				quizChoices.Length < 10 ||
				quizChoices[1].Prompt.Text != "你的人物是?" ||
				quizChoices[2].Prompt.Text != "何为侠？" ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("set_gender", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("set_personality", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("set_personality_random", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("fadein", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("suggest2", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("set_location", out _))
			{
				throw new InvalidOperationException("Bootstrap completed but a representative runtime asset or the configured opening story was not resolvable.");
			}

			GD.Print($"XMJH_BOOTSTRAP_SMOKE mod={loadout.PrimaryMod.ModId} packs={loadout.PrimaryMod.PackFilePaths.Count} mainMenu={GameFlow.MainMenuScenePath}");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"XMJH_BOOTSTRAP_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}
}
