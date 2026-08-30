using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    private const string StoryDirectoryName = "stories";
    private const string StoryFilePattern = "*.story.json";

    private static ContentPackage LoadPackageFromDirectory(string directoryPath, bool required = true)
    {
        if (!Directory.Exists(directoryPath))
        {
            if (!required)
            {
                return new ContentPackage();
            }

            throw new DirectoryNotFoundException($"Content directory '{directoryPath}' was not found.");
        }

        var packageNode = new JsonObject();
        foreach (var spec in ContentTypeCatalog.All)
        {
            packageNode[spec.PackagePropertyName] = LoadDefinitionArray(directoryPath, spec, required);
        }

        var package = packageNode.Deserialize<ContentPackage>(ContentJson)
            ?? throw new InvalidOperationException($"Unable to deserialize content directory '{directoryPath}'.");
        package.StoryScripts = LoadStoryScripts(directoryPath);
        return package;
    }

    private static JsonArray LoadDefinitionArray(string directoryPath, ContentTypeSpec spec, bool required)
    {
        var definitions = new JsonArray();
        // Character titles were introduced after the base schema; keep them optional
        // so existing mods and save fixtures remain loadable with an empty set.
        var sourceRequired = required && !string.Equals(spec.Kind, "characterTitle", StringComparison.Ordinal);
        foreach (var entry in DefinitionSourceReader.Read(directoryPath, spec, sourceRequired))
        {
            definitions.Add(entry.Definition);
        }

        return definitions;
    }

    private static Dictionary<string, StoryScript> LoadStoryScripts(string directoryPath)
    {
        var storyDirectoryPath = Path.Combine(directoryPath, StoryDirectoryName);
        if (!Directory.Exists(storyDirectoryPath))
        {
            return new Dictionary<string, StoryScript>(StringComparer.Ordinal);
        }

        var scripts = new Dictionary<string, StoryScript>(StringComparer.Ordinal);
        var storyPaths = Directory.GetFiles(storyDirectoryPath, StoryFilePattern, SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);

        foreach (var storyPath in storyPaths)
        {
            var scriptId = BuildStoryScriptId(storyDirectoryPath, storyPath);
            Ensure(scripts.TryAdd(scriptId, StoryScriptJson.LoadFromFile(storyPath)),
                $"Story script '{scriptId}' is duplicated.");
        }

        return scripts;
    }

    private static string BuildStoryScriptId(string storyDirectoryPath, string storyPath)
    {
        var relativePath = Path.GetRelativePath(storyDirectoryPath, storyPath)
            .Replace('\\', '/');
        const string suffix = ".story.json";
        Ensure(relativePath.EndsWith(suffix, StringComparison.Ordinal),
            $"Story file '{relativePath}' must end with '{suffix}'.");
        return relativePath[..^suffix.Length];
    }
}
