using Game.Content.Loading;
using Game.Application.Mods;
using Game.Core.Story;
using Game.Expressions;

namespace Game.Tests;

public sealed class XmjhGeneratedContentTests
{
    [Fact]
    public void GeneratedXmjhModDirectoryLoadsStoryIrAndDefinitions()
    {
        var dataDirectory = FindWorkspacePath("mods", "xmjh", "data");
        var repository = new JsonContentLoader().LoadFromDirectory(dataDirectory);

        Assert.True(repository.GetStoryScript("storys") is not null);
        Assert.True(repository.GetStoryScript("storysCG") is not null);
        Assert.True(repository.GetStoryScript("storysPY") is not null);
        Assert.NotNull(repository.GetStorySegment("开局答题"));
        var opening = repository.GetStorySegment("开局答题").Segment;
        var openingPrompts = opening.Steps.OfType<ChoiceStep>().Select(step => step.Prompt.Text).ToArray();
        Assert.Contains("你的人物是?", openingPrompts);
        Assert.Contains("何为侠？", openingPrompts);
        Assert.DoesNotContain("在来到这个世界之前，请允许询问您几个问题", openingPrompts);
        Assert.DoesNotContain("请选择人物头像", openingPrompts);
        var portraitCommands = opening.Steps.OfType<BranchStep>()
            .SelectMany(step => step.Cases)
            .SelectMany(branch => branch.Steps)
            .OfType<CommandStep>()
            .Where(step => step.Call.Root.Name == "select_portrait")
            .Select(step => step.Call.Source)
            .ToArray();
        Assert.Equal([
            "select_portrait('主角', 'xmjh_male')",
            "select_portrait('主角', 'xmjh_female')",
            "select_portrait('主角', 'xmjh_special')",
        ], portraitCommands);
        Assert.NotNull(repository.GetStorySegment("新手村_出生"));
        Assert.NotNull(repository.GetStorySegment("新手村_出生后续"));
        var directPortraitDialogue = Assert.Single(repository.GetStorySegment("天降队友4男").Segment.Steps
            .OfType<DialogueStep>()
            .Where(step => step.Text.Contains("咱们约好出去游历江湖", StringComparison.Ordinal)));
        Assert.Equal("女子", directPortraitDialogue.Speaker);
        Assert.Equal("头像.女主1", directPortraitDialogue.Portrait);
        Assert.Equal("物品.箱子", repository.GetCharacter("箱子").Portrait);
        var chestCommand = Assert.Single(repository.GetStorySegment("侠说_箱子判定").Segment.Steps
            .OfType<BranchStep>()
            .SelectMany(step => step.Cases)
            .SelectMany(branch => branch.Steps)
            .OfType<CommandStep>()
            .Where(step => step.Call.Root.Name == "chest"));
        Assert.Equal("chest()", chestCommand.Call.Source);
        var openingHint = repository.GetStorySegment("开局_提示").Segment;
        var coloredSuggest = Assert.Single(openingHint.Steps.OfType<CommandStep>().Where(step =>
            step.Call.Root.Name == "suggest"));
        Assert.Equal(
            "suggest('[color=red]新手一周目必看！来自神秘少女的留言[/color]')",
            coloredSuggest.Call.Source);
        var villageIntro = repository.GetStorySegment("新手村_小村介绍").Segment;
        var villageExit = Assert.IsType<CommandStep>(villageIntro.Steps[^1]);
        Assert.Equal("map", villageExit.Call.Root.Name);
        var villageMap = Assert.IsType<LiteralExpressionSyntax>(villageExit.Call.Root.Arguments[0]);
        Assert.Equal("遗落世界", villageMap.Value.AsString("village intro map target"));
        Assert.True(repository.TryGetMap("遗落世界", out _));
        Assert.NotEmpty(repository.Characters);
        Assert.NotEmpty(repository.ExternalSkills);
        Assert.NotEmpty(repository.InternalSkills);
        Assert.NotEmpty(repository.Items);
        Assert.NotEmpty(repository.Maps);
        Assert.NotEmpty(repository.CharacterTitles);
        Assert.NotEmpty(repository.Battles);
        Assert.NotEmpty(repository.WorldTriggers);

        var powerGang = repository.GetMap("权力帮");
        var yanKuangtu = Assert.Single(powerGang.Locations.Where(location => location.Id == "燕狂徒"));
        var yanEvent = Assert.Single(yanKuangtu.Events);
        Assert.Equal("story", yanEvent.Action.Root.Name);
        var yanTarget = Assert.IsType<LiteralExpressionSyntax>(yanEvent.Action.Root.Arguments[0]);
        Assert.Equal("侠说_台湾权力帮燕狂徒", yanTarget.Value.AsString("XMJH map event"));
        Assert.NotNull(repository.GetStorySegment("侠说_台湾权力帮燕狂徒"));

        var unknownRoom = repository.GetMap("不知名间");
        var mirror = Assert.Single(unknownRoom.Locations.Where(location => location.Id == "明镜"));
        var mirrorRepeatEvent = Assert.Single(mirror.Events.Where(mapEvent =>
            mapEvent.Action.Root.Name == "story" &&
            mapEvent.Action.Source.Contains("不知名间_镜子", StringComparison.Ordinal) &&
            !mapEvent.Action.Source.Contains("镜子2", StringComparison.Ordinal)));
        Assert.Equal(Game.Core.Definitions.RepeatMode.Once, mirrorRepeatEvent.RepeatMode);
        Assert.Equal(-1, mirrorRepeatEvent.RepeatLimit);

        var endingMap = repository.GetMap("时之狭间");
        var normalEndingEvent = endingMap.Locations
            .SelectMany(location => location.Events)
            .Single(mapEvent => mapEvent.Action.Source == "story('普通结局s')");
        Assert.Equal("story", normalEndingEvent.Action.Root.Name);
        Assert.NotNull(repository.GetStorySegment("普通结局s"));

        var endingSelector = repository.GetStorySegment("普通结局2").Segment;
        var roundBranches = endingSelector.Steps.OfType<BranchStep>().ToArray();
        Assert.Equal(2, roundBranches.Length);
        Assert.Equal("round > 12", roundBranches[0].Cases.Single().When.Source);
        Assert.Equal("round > 7", roundBranches[1].Cases.Single().When.Source);
        Assert.Equal("普通结局2a", Assert.IsType<JumpStep>(endingSelector.Steps[^1]).Target);

        var normalBattle = Assert.IsType<BattleStep>(
            Assert.Single(repository.GetStorySegment("普通结局2a").Segment.Steps));
        Assert.Equal("普通结局胜利", Assert.IsType<JumpStep>(Assert.Single(normalBattle.Outcomes[BattleOutcome.Win])).Target);
        Assert.Equal("普通结局失败", Assert.IsType<JumpStep>(Assert.Single(normalBattle.Outcomes[BattleOutcome.Lose])).Target);

        var home = repository.GetMap("大厅");
        var sharedHomeStoryEvents = home.Locations
            .Where(location => location.Id is "软体二代" or "南贤二代" or "北丑二代")
            .SelectMany(location => location.Events)
            .Where(mapEvent => mapEvent.Action.Source == "story('家剧情')")
            .ToArray();
        Assert.Equal(3, sharedHomeStoryEvents.Length);
        Assert.All(sharedHomeStoryEvents, mapEvent => Assert.Equal(
            Game.Core.Definitions.RepeatMode.Once,
            mapEvent.RepeatMode));

        var completedHomeState = new Game.Core.Model.GameState();
        // Legacy judges the 天书 branch against the shared 50-default favorability
        // store; a real playthrough always reaches 大厅 after the opening story
        // zeroed these counters (新手村_出生后续).  Model that post-opening state
        // so the branch stays dormant exactly like the shipped game.
        completedHomeState.Adventure.ChangeFavorability("天书", -50);
        completedHomeState.Story.MarkCompleted("家剧情");
        var completedHomeSession = new Game.Application.GameSession(completedHomeState, repository);
        var completedHomeLocations = completedHomeSession.MapService.EnterMap("大厅").Locations;
        Assert.DoesNotContain(
            completedHomeLocations,
            location => location.Event?.Action.Source == "story('家剧情')");
        var completedSouthHomeEvent = completedHomeLocations
            .Single(location => location.Location.Id == "南贤二代")
            .Event;
        Assert.Equal("story('南北居_南贤二代')", completedSouthHomeEvent?.Action.Source);

        Assert.Contains(repository.GetTalent("轻功大师").Affixes, affix =>
            affix is Game.Core.Affix.TraitAffix { TraitId: Game.Core.Affix.TraitId.IgnoreZoneOfControl });
        Assert.Contains(repository.GetTalent("练武奇才").Affixes, affix =>
            affix is Game.Core.Affix.TraitAffix { TraitId: Game.Core.Affix.TraitId.DoubleExperienceGain });
        Assert.Contains(repository.GetTalent("百毒不侵").Affixes, affix =>
            affix is Game.Core.Affix.TraitAffix { TraitId: Game.Core.Affix.TraitId.PoisonImmunity });

        var sampleBattle = repository.Battles.Values.FirstOrDefault(b => b.Participants.Count > 0);
        Assert.NotNull(sampleBattle);
        Assert.All(sampleBattle.Participants, participant =>
        {
            Assert.InRange(participant.Position.X, 0, 12);
            Assert.InRange(participant.Position.Y, 0, 4);
        });
    }

    [Fact]
    public void XmjhPrimaryConfigPointsToAnAvailableOpeningStory()
    {
        var projectRoot = FindWorkspacePath();
        var loaded = new JsonContentLoader().LoadModContent([
            new ModContentInput("xmjh", Path.Combine(projectRoot, "mods", "xmjh"), Required: true),
        ]);

        Assert.Equal("开局答题", loaded.Config.InitialStorySegmentId);
        Assert.NotNull(loaded.Repository.GetStorySegment(loaded.Config.InitialStorySegmentId));
    }

    [Fact]
    public void LauncherDiscoversXmjhAndResolvesItAsPrimaryGameMod()
    {
        var projectRoot = FindWorkspacePath();
        var dataRoot = new ProjectDataRoot(projectRoot);
        var mods = new ModRegistry(dataRoot).DiscoverMods();

        Assert.Contains(mods, mod => mod.ModId == "xmjh");
        var settings = new LauncherSettingsStore(dataRoot.LauncherSettingsPath).LoadOrEmpty();
        Assert.Equal("xmjh", settings.PrimaryModId);

        var loadout = new ModLoadoutResolver(mods).Resolve(settings.PrimaryModId!, settings.EnabledAddonIds);
        Assert.Equal("xmjh", loadout.PrimaryMod.ModId);
        Assert.Empty(loadout.AddonMods);
    }

    private static string FindWorkspacePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (Directory.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(string.Join(Path.DirectorySeparatorChar, segments));
    }

    private static string FindWorkspacePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "project.godot")) &&
                Directory.Exists(Path.Combine(directory.FullName, "mods")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Godot workspace root");
    }
}
