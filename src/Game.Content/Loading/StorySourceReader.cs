using Game.Core.Story;
using Game.StoryDsl;

namespace Game.Content.Loading;

internal sealed record StorySourceEntry(string ScriptId, string FilePath, StoryScript Script);

internal static class StorySourceReader
{
    public static IEnumerable<StorySourceEntry> Read(string dataDirectoryPath)
    {
        var directory = Path.Combine(dataDirectoryPath, "stories");
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var paths = Directory.GetFiles(directory, "*.story", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(directory, path).Replace('\\', '/'), StringComparer.Ordinal);
        foreach (var path in paths)
        {
            StoryScript script;
            try
            {
                script = StoryCompiler.Compile(File.ReadAllText(path), path);
            }
            catch (StoryCompilationException exception)
            {
                throw new ContentLoadException(exception.Message, exception);
            }

            var relativePath = Path.GetRelativePath(directory, path).Replace('\\', '/');
            yield return new StorySourceEntry(relativePath[..^".story".Length], path, script);
        }
    }
}
