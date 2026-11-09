using System.IO.Compression;
using Game.Application.Mods;

namespace Game.Tests;

public sealed class ModArchiveImporterTests : IDisposable
{
    private readonly ProjectDataRoot _root = ProjectDataRoot.FromPath(
        Path.Combine(AppContext.BaseDirectory, "import-tests", Guid.NewGuid().ToString("N")));
    private const string Manifest = """{"id":"demo","name":"Demo","version":"1","type":"addon","dependencies":[],"saveImpact":"none"}""";

    [Theory]
    [InlineData("")]
    [InlineData("demo/")]
    public void ImportsRootOrFolderArchiveAndDiscoversMod(string prefix)
    {
        using var zip = CreateZip((prefix + "mod.json", Manifest), (prefix + "patches/test.json", "[]"));
        var ids = new ModArchiveImporter(_root).Import(zip);
        Assert.Equal("demo", Assert.Single(ids));
        Assert.Equal("demo", Assert.Single(new ModRegistry(_root).DiscoverMods()).ModId);
        Assert.True(File.Exists(Path.Combine(_root.ModsDirectoryPath, "demo", "patches", "test.json")));
        Assert.Empty(Directory.GetFileSystemEntries(_root.LauncherDirectoryPath));
    }

    [Fact]
    public void ImportsMultipleModsUsingManifestIds()
    {
        using var zip = CreateZip(("folder-a/mod.json", Manifest),
            ("folder-b/mod.json", Manifest.Replace("\"demo\"", "\"second\"")));
        Assert.Equal(["demo", "second"], new ModArchiveImporter(_root).Import(zip));
        Assert.Equal(2, new ModRegistry(_root).DiscoverMods().Count);
    }

    [Fact]
    public void RejectsDuplicateManifestIds()
    {
        using var zip = CreateZip(("a/mod.json", Manifest), ("b/mod.json", Manifest));
        Assert.Throws<InvalidDataException>(() => new ModArchiveImporter(_root).Import(zip));
        Assert.False(Directory.Exists(_root.ModsDirectoryPath));
    }

    [Fact]
    public void RejectsParentDirectoryManifestId()
    {
        using var zip = CreateZip(("mod.json", Manifest.Replace("\"demo\"", "\"..\"")));
        Assert.Throws<InvalidDataException>(() => new ModArchiveImporter(_root).Import(zip));
    }

    [Fact]
    public void InvalidZipLeavesNoPartialInstallation()
    {
        using var zip = new MemoryStream([1, 2, 3]);
        Assert.Throws<InvalidDataException>(() => new ModArchiveImporter(_root).Import(zip));
        Assert.Empty(Directory.GetFileSystemEntries(_root.LauncherDirectoryPath));
    }

    [Fact]
    public void ReplacementRemovesOldFilesAndPreservesPlayerData()
    {
        var mod = Path.Combine(_root.ModsDirectoryPath, "demo");
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, "obsolete.txt"), "old");
        Directory.CreateDirectory(_root.UserDataDirectoryPath);
        var save = Path.Combine(_root.UserDataDirectoryPath, "save.json");
        File.WriteAllText(save, "player data");
        using var zip = CreateZip(("mod.json", Manifest));
        new ModArchiveImporter(_root).Import(zip);
        Assert.False(File.Exists(Path.Combine(mod, "obsolete.txt")));
        Assert.Equal("player data", File.ReadAllText(save));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("/escape.txt")]
    [InlineData("demo/../../escape.txt")]
    [InlineData("demo\\..\\escape.txt")]
    [InlineData("C:/escape.txt")]
    public void RejectsUnsafePathsWithoutReplacingInstalledMod(string path)
    {
        var mod = Path.Combine(_root.ModsDirectoryPath, "demo");
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, "keep.txt"), "old");
        using var zip = CreateZip(("demo/mod.json", Manifest), (path, "bad"));
        Assert.Throws<InvalidDataException>(() => new ModArchiveImporter(_root).Import(zip));
        Assert.Equal("old", File.ReadAllText(Path.Combine(mod, "keep.txt")));
        Assert.Empty(Directory.GetFileSystemEntries(_root.LauncherDirectoryPath));
    }

    [Fact]
    public void InvalidModInBundlePreventsAllReplacements()
    {
        using var zip = CreateZip(("demo/mod.json", Manifest), ("broken/mod.json", "{}"));
        Assert.Throws<InvalidOperationException>(() => new ModArchiveImporter(_root).Import(zip));
        Assert.False(Directory.Exists(Path.Combine(_root.ModsDirectoryPath, "demo")));
    }

    [Fact]
    public void InstallationFailureRollsBackEarlierReplacement()
    {
        var mod = Path.Combine(_root.ModsDirectoryPath, "demo");
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, "keep.txt"), "old");
        File.WriteAllText(Path.Combine(_root.ModsDirectoryPath, "second"), "blocks destination");
        using var zip = CreateZip(("a/mod.json", Manifest),
            ("b/mod.json", Manifest.Replace("\"demo\"", "\"second\"")));
        Assert.ThrowsAny<IOException>(() => new ModArchiveImporter(_root).Import(zip));
        Assert.Equal("old", File.ReadAllText(Path.Combine(mod, "keep.txt")));
        Assert.Empty(Directory.GetFileSystemEntries(_root.LauncherDirectoryPath));
    }

    [Fact]
    public void RejectsSymlinkEntries()
    {
        using var zip = CreateZip(("mod.json", Manifest));
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = archive.CreateEntry("link");
            entry.ExternalAttributes = unchecked((int)0xA1FF0000);
        }
        zip.Position = 0;
        Assert.Throws<InvalidDataException>(() => new ModArchiveImporter(_root).Import(zip));
    }

    private static MemoryStream CreateZip(params (string Path, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in files)
            {
                using var writer = new StreamWriter(archive.CreateEntry(path).Open());
                writer.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root.Path)) Directory.Delete(_root.Path, recursive: true);
    }
}
