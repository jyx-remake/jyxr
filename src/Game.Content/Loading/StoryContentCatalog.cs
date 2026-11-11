using Game.Core.Story;

namespace Game.Content.Loading;

internal sealed class StoryContentCatalog(ICollection<ContentLoadWarning> warnings)
{
    private readonly Dictionary<string, StoryScript> _scripts = new(StringComparer.Ordinal);
    private readonly Dictionary<(string ModId, string SegmentId), string> _sourceFiles = [];
    private readonly Dictionary<string, SegmentSource> _segments = new(StringComparer.Ordinal);

    public void AddMod(ModContentInput input)
    {
        foreach (var source in StorySourceReader.Read(input.DataDirectoryPath))
        {
            var scriptId = $"{input.ModId}/{source.ScriptId}";
            _scripts.Add(scriptId, source.Script);
            foreach (var segment in source.Script.Segments)
            {
                if (!_sourceFiles.TryAdd((input.ModId, segment.Name), source.FilePath))
                {
                    var previousPath = _sourceFiles[(input.ModId, segment.Name)];
                    throw new ContentLoadException(
                        $"Story segment '{segment.Name}' in '{source.FilePath}' is duplicated in mod '{input.ModId}', previously defined in '{previousPath}'.");
                }

                if (_segments.TryGetValue(segment.Name, out var previous))
                {
                    warnings.Add(new ContentLoadWarning(
                        $"story:{segment.Name}",
                        previous.ModId,
                        input.ModId,
                        $"Story segment '{segment.Name}' from mod '{input.ModId}' in '{source.FilePath}' replaces the segment from mod '{previous.ModId}' in '{previous.FilePath}'."));
                }

                _segments[segment.Name] = new SegmentSource(scriptId, input.ModId, source.FilePath);
            }
        }
    }

    public Dictionary<string, StoryScript> BuildScripts()
    {
        return _scripts.ToDictionary(
            static entry => entry.Key,
            entry => new StoryScript(
                entry.Value.Segments.Where(segment => _segments[segment.Name].ScriptId == entry.Key).ToArray()),
            StringComparer.Ordinal);
    }

    private sealed record SegmentSource(string ScriptId, string ModId, string FilePath);
}
