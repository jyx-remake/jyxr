using Game.Application.Mods;
using Game.Core.Story;
using Game.Core.Definitions;
using Game.Expressions;
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

	private async void RunSmoke()
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
			var introStory = Game.ContentRepository.GetStorySegment("新手村_小村介绍").Segment;
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
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("remove_item", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("story", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("map", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("fadein", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("suggest2", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("story_by_hero_name", out _) ||
				!Game.StoryService.CommandDispatcher.Registry.TryGetDescriptor("set_location", out _) ||
				!Game.ContentRepository.TryGetMap("遗落世界", out _) ||
				introStory.Steps.LastOrDefault() is not CommandStep { Call.Root.Name: "map" })
			{
				throw new InvalidOperationException("Bootstrap completed but a representative runtime asset or the configured opening story was not resolvable.");
			}

			// Exercise the exact migrated calls that previously failed in-game.
			// The legacy branch suffix is intentional: rollrole.lua grants the
			// shared base token while old story XML consumes 队友表决令3/4.
			var voteToken = Game.ContentRepository.GetItem("队友表决令");
			Game.InventoryService.AddItem(voteToken, notifyAcquisition: false);
			var parser = new ExpressionParser();
			await Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall("remove_item('队友表决令3', 1, false)"));
			if (Game.State.Inventory.ContainsStack(voteToken))
			{
				throw new InvalidOperationException("remove_item did not consume the legacy suffixed vote token alias.");
			}
			await Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall("remove_item('队友表决令4', 1, false)"));

			const string noOpStoryId = "襄阳说书人b";
			await Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall($"story('{noOpStoryId}')"));
			if (!Game.State.Story.IsStoryCompleted(noOpStoryId))
			{
				throw new InvalidOperationException("story command did not execute a generated XMJH segment.");
			}

			await Game.StoryService.CommandDispatcher.ExecuteCallAsync(
				parser.ParseCall("map('遗落世界')"));
			if (Game.State.Location.CurrentMapId != "遗落世界")
			{
				throw new InvalidOperationException("map command did not enter 遗落世界.");
			}

			var mirrorLocation = Game.ContentRepository.GetMap("不知名间")
				.Locations.Single(location => location.Id == "明镜");
			var mirrorEvent = mirrorLocation.Events.Single(mapEvent =>
				mapEvent.RepeatMode == RepeatMode.Once && mapEvent.RepeatLimit == -1);
			for (var occurrence = 1; occurrence <= 2; occurrence++)
			{
				var location = Game.MapService.EnterMap("不知名间").Locations.Single(candidate =>
					candidate.Location.Id == "明镜" && candidate.Event?.Id == mirrorEvent.Id);
				Game.MapService.CompleteInteraction(Game.MapService.InteractWithLocation(location));
			}
			if (Game.State.MapEventProgress.GetOccurrenceCount("不知名间", "明镜", mirrorEvent.Id) != 2 ||
				Game.MapService.EnterMap("不知名间").Locations.All(candidate =>
					candidate.Location.Id != "明镜" || candidate.Event?.Id != mirrorEvent.Id))
			{
				throw new InvalidOperationException("once,-1 mirror event did not remain available after repeated completion.");
			}

			GD.Print($"XMJH_BOOTSTRAP_SMOKE mod={loadout.PrimaryMod.ModId} packs={loadout.PrimaryMod.PackFilePaths.Count} quiz=xmjh remove_item=ok story=ok once=-1 map={Game.State.Location.CurrentMapId} mainMenu={GameFlow.MainMenuScenePath}");
			Quit(0);
		}
		catch (Exception exception)
		{
			GD.PrintErr($"XMJH_BOOTSTRAP_SMOKE_FAILED {exception}");
			Quit(1);
		}
	}
}
