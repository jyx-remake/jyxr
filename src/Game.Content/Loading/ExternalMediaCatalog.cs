namespace Game.Content.Loading;

public sealed class ExternalMediaCatalog
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, string>> _layers;

    public ExternalMediaCatalog(IEnumerable<string> modDirectoriesInLoadOrder)
    {
        var directories = new[]
        {
            (Name: "art", Extensions: MediaReferenceResolver.RasterTextureExtensions),
            (Name: "audio", Extensions: MediaReferenceResolver.AudioExtensions),
        };
        var layers = new List<IReadOnlyDictionary<string, string>>();
        foreach (var modDirectory in modDirectoriesInLoadOrder)
        {
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (name, extensions) in directories)
            {
                var directory = Path.Combine(modDirectory, "assets", name);
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    IgnoreInaccessible = false,
                };
                foreach (var file in Directory.EnumerateFiles(directory, "*", options))
                {
                    if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    {
                        var relativePath = Path.GetRelativePath(directory, file).Replace('\\', '/');
                        files.Add($"{name}/{relativePath}", Path.GetFullPath(file));
                    }
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
