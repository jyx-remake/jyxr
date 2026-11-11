using Game.Content.Loading;

namespace Game.Tests;

public sealed class ExternalTextureCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "texture-tests", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("head/hero.png")]
    [InlineData("item/sword.jpg")]
    [InlineData("icon/skill.jpeg")]
    [InlineData("map/village.webp")]
    [InlineData("battle_bg/custom/arena.png")]
    public void ResolvesExternalRasterImages(string relativePath)
    {
        var file = WriteAsset("base", relativePath);
        var catalog = CreateCatalog("base");

        Assert.Equal(file, catalog.Resolve([$"art/{relativePath}"]));
    }

    [Fact]
    public void LaterModWinsBeforeExtensionPreference()
    {
        var original = WriteAsset("base", "head/hero.png");
        var replacement = WriteAsset("addon", "head/hero.webp");
        var catalog = CreateCatalog("base", "addon");
        var candidates = MediaReferenceResolver.GetCandidateAssetPaths("art/head/hero", MediaAssetKind.Texture);

        Assert.Equal(replacement, catalog.Resolve(candidates));
        Assert.Equal(original, catalog.Resolve(["art/head/hero.png"]));
    }

    [Fact]
    public void UsesCandidateOrderWithinEachMod()
    {
        var preferred = WriteAsset("base", "head/hero.png");
        WriteAsset("base", "head/hero.jpg");
        var catalog = CreateCatalog("base");

        Assert.Equal(preferred, catalog.Resolve(["art/head/hero.png", "art/head/hero.jpg"]));
    }

    [Fact]
    public void IndexesOnlyEnabledModsAndRasterFiles()
    {
        WriteAsset("disabled", "head/hero.png");
        WriteAsset("base", "head/hero.tres");
        WriteAsset("base", "head/hero.res");
        var catalog = CreateCatalog("base", "empty");

        Assert.Null(catalog.Resolve(["art/head/hero.png", "art/head/hero.tres", "art/head/hero.res"]));
        Assert.Null(catalog.Resolve(["art/../head/hero.png"]));
    }

    [Fact]
    public void RebuildingCatalogPicksUpNewFiles()
    {
        var catalog = CreateCatalog("base");
        var file = WriteAsset("base", "head/hero.png");

        Assert.Null(catalog.Resolve(["art/head/hero.png"]));
        Assert.Equal(file, CreateCatalog("base").Resolve(["art/head/hero.png"]));
    }

    private ExternalTextureCatalog CreateCatalog(params string[] mods) =>
        new(mods.Select(mod => Path.Combine(_root, mod)));

    private string WriteAsset(string mod, string relativePath)
    {
        var file = Path.Combine(_root, mod, "assets", "art", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, []);
        return file;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
