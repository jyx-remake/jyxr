namespace Game.Content.Loading;

public sealed class ExternalTextureCatalog
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, string>> _layers;

    public ExternalTextureCatalog(IEnumerable<string> modDirectoriesInLoadOrder)
    {
        var layers = new List<IReadOnlyDictionary<string, string>>();
        foreach (var modDirectory in modDirectoriesInLoadOrder)
        {
            var artDirectory = Path.Combine(modDirectory, "assets", "art");
            if (!Directory.Exists(artDirectory))
            {
                continue;
            }

            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
                IgnoreInaccessible = false,
            };
            foreach (var file in Directory.EnumerateFiles(artDirectory, "*", options))
            {
                if (MediaReferenceResolver.RasterTextureExtensions.Contains(
                        Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    var relativePath = Path.GetRelativePath(artDirectory, file).Replace('\\', '/');
                    files.Add($"art/{relativePath}", Path.GetFullPath(file));
                }
            }

            layers.Add(files);
        }

        _layers = layers;
    }

    public string? Resolve(IReadOnlyList<string> candidateAssetPaths)
    {
        for (var index = _layers.Count - 1; index >= 0; index--)
        {
            foreach (var candidate in candidateAssetPaths)
            {
                if (_layers[index].TryGetValue(candidate, out var path))
                {
                    return path;
                }
            }
        }

        return null;
    }
}
