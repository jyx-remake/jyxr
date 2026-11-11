using Game.Content.Loading;

namespace Game.Tests;

public sealed class ExternalMediaCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "media-tests", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("art/head/hero.png")]
    [InlineData("art/item/sword.jpg")]
    [InlineData("art/icon/skill.jpeg")]
    [InlineData("art/map/village.webp")]
    [InlineData("art/battle_bg/custom/arena.png")]
    [InlineData("audio/theme.ogg")]
    [InlineData("audio/bgm/theme.mp3")]
    [InlineData("audio/sfx/hit.wav")]
    public void ResolvesExternalMedia(string relativePath)
    {
        var file = WriteAsset("base", relativePath);
        var catalog = CreateCatalog("base");

        Assert.Equal(file, catalog.Resolve([relativePath]));
    }

    [Fact]
    public void LaterModWinsBeforeExtensionPreference()
    {
        var original = WriteAsset("base", "art/head/hero.png");
        var replacement = WriteAsset("addon", "art/head/hero.webp");
        var catalog = CreateCatalog("base", "addon");
        var candidates = MediaReferenceResolver.GetCandidateAssetPaths("art/head/hero", MediaAssetKind.Texture);

        Assert.Equal(replacement, catalog.Resolve(candidates));
        Assert.Equal(original, catalog.Resolve(["art/head/hero.png"]));
    }

    [Fact]
    public void UsesCandidateOrderWithinEachMod()
    {
        var preferred = WriteAsset("base", "art/head/hero.png");
        WriteAsset("base", "art/head/hero.jpg");
        var catalog = CreateCatalog("base");

        Assert.Equal(preferred, catalog.Resolve(["art/head/hero.png", "art/head/hero.jpg"]));
    }

    [Fact]
    public void IndexesOnlyEnabledModsAndSupportedMedia()
    {
        WriteAsset("disabled", "art/head/hero.png");
        WriteAsset("disabled", "audio/theme.ogg");
        WriteAsset("base", "art/head/hero.tres");
        WriteAsset("base", "art/head/hero.res");
        WriteAsset("base", "audio/theme.flac");
        WriteAsset("base", "audio/hero.png");
        WriteAsset("base", "art/theme.ogg");
        var catalog = CreateCatalog("base", "empty");

        Assert.Null(catalog.Resolve(["art/head/hero.png", "art/head/hero.tres", "art/head/hero.res"]));
        Assert.Null(catalog.Resolve(["art/../head/hero.png"]));
        Assert.Null(catalog.Resolve(["audio/theme.ogg", "audio/theme.flac", "audio/hero.png", "art/theme.ogg"]));
    }

    [Fact]
    public void RebuildingCatalogPicksUpNewFiles()
    {
        var catalog = CreateCatalog("base");
        var file = WriteAsset("base", "art/head/hero.png");

        Assert.Null(catalog.Resolve(["art/head/hero.png"]));
        Assert.Equal(file, CreateCatalog("base").Resolve(["art/head/hero.png"]));
    }

    [Fact]
    public void AudioOverridesFollowModOrderBeforeExtensionPreference()
    {
        var original = WriteAsset("base", "audio/theme.ogg");
        var replacement = WriteAsset("addon", "audio/theme.wav");
        var catalog = CreateCatalog("base", "addon");

        Assert.Equal(replacement, catalog.Resolve(
            MediaReferenceResolver.GetCandidateAssetPaths("audio/theme", MediaAssetKind.Audio)));
        Assert.Equal(original, catalog.Resolve(["audio/theme.ogg"]));
    }

    private ExternalMediaCatalog CreateCatalog(params string[] mods) =>
        new(mods.Select(mod => Path.Combine(_root, mod)));

    private string WriteAsset(string mod, string relativePath)
    {
        var file = Path.Combine(_root, mod, "assets", relativePath.Replace('/', Path.DirectorySeparatorChar));
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
