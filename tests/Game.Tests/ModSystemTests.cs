using System.Text.Json;
using Game.Application.Mods;
using Game.Content.Loading;
using Game.Core.Model;
using Game.Core.Serialization;

namespace Game.Tests;

public sealed class ModSystemTests
{
    private static string BaseProjectDataRootPath =>
        Path.Combine(AppContext.BaseDirectory);

    private static string BaseModDataPath =>
        Path.Combine(BaseProjectDataRootPath, "mods", "jyxr-base", "data");

    [Fact]
    public void ModRegistry_DiscoversBaseModFromProjectDataRoot()
    {
        var root = ProjectDataRoot.FromPath(BaseProjectDataRootPath);
        var mods = new ModRegistry(root).DiscoverMods();

        var mod = Assert.Single(mods, candidate => candidate.ModId == "jyxr-base");
        Assert.Equal(Path.Combine(root.ModsDirectoryPath, "jyxr-base", "data"), mod.DataDirectoryPath);
    }

    [Fact]
    public void ModRegistry_RejectsInvalidManifest()
    {
        var tempRoot = CreateTempProjectDataRoot();
        var modDirectory = Path.Combine(tempRoot.Path, "mods", "bad mod");
        Directory.CreateDirectory(Path.Combine(modDirectory, "data"));
        File.WriteAllText(Path.Combine(modDirectory, "mod.json"), """{"id":"bad mod","name":"Bad","version":"1"}""");

        Assert.Throws<InvalidOperationException>(() => ModRegistry.LoadMod(tempRoot, modDirectory));
    }

    [Fact]
    public void ModRegistry_RejectsLooseAssetManifestField()
    {
        var tempRoot = CreateTempProjectDataRoot();
        var modDirectory = Path.Combine(tempRoot.Path, "mods", "loose-assets");
        Directory.CreateDirectory(Path.Combine(modDirectory, "data"));
        File.WriteAllText(
            Path.Combine(modDirectory, "mod.json"),
            """{"id":"loose-assets","name":"Loose Assets","version":"1","assetsPath":"assets"}""");

        Assert.Throws<InvalidOperationException>(() => ModRegistry.LoadMod(tempRoot, modDirectory));
    }

    [Fact]
    public void ModRegistry_ResolvesPackPathsInManifestOrder()
    {
        var tempRoot = CreateTempProjectDataRoot();
        var modDirectory = Path.Combine(tempRoot.Path, "mods", "packed");
        Directory.CreateDirectory(Path.Combine(modDirectory, "data"));
        Directory.CreateDirectory(Path.Combine(modDirectory, "packs"));
        File.WriteAllText(Path.Combine(modDirectory, "packs", "base.pck"), "");
        File.WriteAllText(Path.Combine(modDirectory, "packs", "ui.pck"), "");
        File.WriteAllText(
            Path.Combine(modDirectory, "mod.json"),
            """{"id":"packed","name":"Packed","version":"1","packs":["packs/base.pck","packs/ui.pck"]}""");

        var context = ModRegistry.LoadMod(tempRoot, modDirectory);

        Assert.Equal(
            [
                Path.Combine(modDirectory, "packs", "base.pck"),
                Path.Combine(modDirectory, "packs", "ui.pck"),
            ],
            context.PackFilePaths);
    }

    [Fact]
    public void ModRegistry_RejectsMissingPack()
    {
        var tempRoot = CreateTempProjectDataRoot();
        var modDirectory = Path.Combine(tempRoot.Path, "mods", "missing-pack");
        Directory.CreateDirectory(Path.Combine(modDirectory, "data"));
        File.WriteAllText(
            Path.Combine(modDirectory, "mod.json"),
            """{"id":"missing-pack","name":"Missing Pack","version":"1","packs":["packs/missing.pck"]}""");

        Assert.Throws<FileNotFoundException>(() => ModRegistry.LoadMod(tempRoot, modDirectory));
    }

    [Fact]
    public void ModRegistry_RejectsUnsupportedPackExtension()
    {
        var tempRoot = CreateTempProjectDataRoot();
        var modDirectory = Path.Combine(tempRoot.Path, "mods", "bad-pack");
        Directory.CreateDirectory(Path.Combine(modDirectory, "data"));
        Directory.CreateDirectory(Path.Combine(modDirectory, "packs"));
        File.WriteAllText(Path.Combine(modDirectory, "packs", "bad.txt"), "");
        File.WriteAllText(
            Path.Combine(modDirectory, "mod.json"),
            """{"id":"bad-pack","name":"Bad Pack","version":"1","packs":["packs/bad.txt"]}""");

        Assert.Throws<InvalidOperationException>(() => ModRegistry.LoadMod(tempRoot, modDirectory));
    }

    [Fact]
    public void ModStoragePaths_IsolateUserDataByModId()
    {
        var alpha = new ModStoragePaths("C:\\project-data", "alpha");
        var beta = new ModStoragePaths("C:\\project-data", "beta");

        Assert.NotEqual(alpha.GetSaveSlotPath(1), beta.GetSaveSlotPath(1));
        Assert.NotEqual(alpha.ProfilePath, beta.ProfilePath);
        Assert.NotEqual(alpha.SettingsPath, beta.SettingsPath);
        Assert.EndsWith(Path.Combine("userdata", "alpha", "saves", "save-slot-1.json"), alpha.GetSaveSlotPath(1));
        Assert.EndsWith(Path.Combine("userdata", "alpha", "saves", "quicksave.json"), alpha.QuickSavePath);
    }

    [Fact]
    public void JsonContentLoader_LoadsBaseModDataDirectory()
    {
        var repository = new JsonContentLoader().LoadFromDirectory(BaseModDataPath);

        Assert.NotNull(repository.GetCharacter("主角"));
        Assert.NotNull(repository.GetMap("大地图"));
    }

    [Fact]
    public void GameConfig_LoadsFromBaseModDataDirectory()
    {
        var configPath = Path.Combine(BaseModDataPath, "game-config.json");
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<GameConfig>(json, GameJson.Default);

        Assert.NotNull(config);
        Assert.NotEmpty(config.InitialPartyCharacterIds);
        Assert.False(string.IsNullOrWhiteSpace(config.InitialStorySegmentId));
    }

    private static ProjectDataRoot CreateTempProjectDataRoot()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestScratch", "jyxr-mod-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return ProjectDataRoot.FromPath(path);
    }
}
