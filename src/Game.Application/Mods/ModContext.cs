namespace Game.Application.Mods;

public sealed record ModContext(
    ProjectDataRoot ProjectDataRoot,
    string ModDirectoryPath,
    ModManifest Manifest)
{
    public string ModId => Manifest.Id;
    public string DataDirectoryPath => Path.Combine(ModDirectoryPath, ModManifest.DataDirectoryName);
    public IReadOnlyList<string> PackFilePaths =>
        Manifest.ResolvedPacks
            .Select(path => Path.Combine(ModDirectoryPath, path.Replace('/', Path.DirectorySeparatorChar)))
            .ToArray();

    public ModStoragePaths StoragePaths => new(ProjectDataRoot.Path, ModId);
}
