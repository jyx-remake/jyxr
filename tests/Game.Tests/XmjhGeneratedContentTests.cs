using Game.Content.Loading;
using Game.Application.Mods;
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
        Assert.NotNull(repository.GetStorySegment("新手村_出生"));
        Assert.NotNull(repository.GetStorySegment("新手村_出生后续"));
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
