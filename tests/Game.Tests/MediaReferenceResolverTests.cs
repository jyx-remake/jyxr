using Game.Content.Loading;
using Game.Core.Definitions;

namespace Game.Tests;

public sealed class MediaReferenceResolverTests
{
    [Theory]
    [InlineData("icon/foo", MediaAssetKind.Texture, "art/icon/foo")]
    [InlineData("item/weapons/jian", MediaAssetKind.Texture, "art/item/weapons/jian")]
    [InlineData("audio/theme", MediaAssetKind.Audio, "audio/theme")]
    [InlineData("video/opening", MediaAssetKind.Video, "video/opening")]
    [InlineData("video/opening.ogv", MediaAssetKind.Video, "video/opening.ogv")]
    public void Resolve_AcceptsTypedPaths(
        string reference,
        MediaAssetKind assetKind,
        string expectedAssetPath)
    {
        var result = MediaReferenceResolver.Resolve(
            reference,
            assetKind,
            TestContentFactory.CreateRepository());

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(MediaReferenceKind.Path, result.ReferenceKind);
        Assert.Equal(expectedAssetPath, result.AssetPath);
    }

    [Fact]
    public void Resolve_ResolvesResourceIdBeforeItsTypedValue()
    {
        var repository = TestContentFactory.CreateRepository(resources:
        [
            new ResourceDefinition { Id = "shared", Value = "item/jian" },
        ]);

        var result = MediaReferenceResolver.Resolve("shared", MediaAssetKind.Texture, repository);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(MediaReferenceKind.ResourceId, result.ReferenceKind);
        Assert.Equal("art/item/jian", result.AssetPath);
    }

    [Theory]
    [InlineData("missing", MediaAssetKind.Texture)]
    [InlineData("assets/art/icon/foo", MediaAssetKind.Texture)]
    [InlineData("art/icon/foo", MediaAssetKind.Texture)]
    [InlineData("audio/foo", MediaAssetKind.Texture)]
    [InlineData("video/foo", MediaAssetKind.Texture)]
    [InlineData("icon/foo", MediaAssetKind.Audio)]
    [InlineData("audio/foo", MediaAssetKind.Video)]
    [InlineData("res://assets/art/icon/foo", MediaAssetKind.Texture)]
    [InlineData("C:/art/icon/foo", MediaAssetKind.Texture)]
    [InlineData("icon/../foo", MediaAssetKind.Texture)]
    [InlineData("icon//foo", MediaAssetKind.Texture)]
    [InlineData("icon\\foo", MediaAssetKind.Texture)]
    [InlineData("audio/foo.exe", MediaAssetKind.Audio)]
    [InlineData("video/foo.mp4", MediaAssetKind.Video)]
    public void Resolve_RejectsInvalidReferences(string reference, MediaAssetKind assetKind)
    {
        var result = MediaReferenceResolver.Resolve(
            reference,
            assetKind,
            TestContentFactory.CreateRepository());

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Error!);
    }

    [Fact]
    public void Resolve_RejectsResourceValueForWrongMediaType()
    {
        var repository = TestContentFactory.CreateRepository(resources:
        [
            new ResourceDefinition { Id = "music", Value = "audio/theme" },
        ]);

        var result = MediaReferenceResolver.Resolve("music", MediaAssetKind.Texture, repository);

        Assert.False(result.IsSuccess);
        Assert.Equal(MediaReferenceKind.ResourceId, result.ReferenceKind);
    }

    [Fact]
    public void GetCandidateAssetPaths_ProbesOnlyWhenExtensionIsOmitted()
    {
        Assert.Equal(
            ["audio/theme.ogg", "audio/theme.mp3", "audio/theme.wav", "audio/theme.flac"],
            MediaReferenceResolver.GetCandidateAssetPaths("audio/theme", MediaAssetKind.Audio));
        Assert.Equal(
            ["audio/theme.mp3"],
            MediaReferenceResolver.GetCandidateAssetPaths("audio/theme.mp3", MediaAssetKind.Audio));
        Assert.Equal(
            ["video/opening.ogv"],
            MediaReferenceResolver.GetCandidateAssetPaths("video/opening", MediaAssetKind.Video));
    }

    [Fact]
    public void JsonLoader_RejectsResourceIdContainingSlash()
    {
        var package = new ContentPackage
        {
            Resources = [new ResourceDefinition { Id = "icon/foo", Value = "icon/foo" }],
        };

        Assert.Throws<InvalidOperationException>(() => new JsonContentLoader().LoadFromPackage(package));
    }

    [Fact]
    public void JsonLoader_LoadsRealBaseModWithUnifiedMediaReferences()
    {
        var repositoryRoot = FindRepositoryRoot();
        var modDirectory = Path.Combine(repositoryRoot, "mods", "jyxr-base");

        var loaded = new JsonContentLoader().LoadModContent(
        [
            new ModContentInput("jyxr-base", modDirectory, Required: true),
        ]);

        Assert.NotEmpty(loaded.Repository.Battles);
        Assert.All(loaded.Repository.Battles.Values,
            battle => Assert.StartsWith("battle_bg/", battle.Background, StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "engine-free-rpg.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
