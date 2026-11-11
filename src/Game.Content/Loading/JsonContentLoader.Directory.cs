using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    private static ContentPackage LoadPackageFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Content directory '{directoryPath}' was not found.");
        }

        var packageNode = new JsonObject();
        foreach (var spec in ContentTypeCatalog.All)
        {
            packageNode[spec.PackagePropertyName] = LoadDefinitionArray(directoryPath, spec);
        }

        var package = packageNode.Deserialize<ContentPackage>(ContentJson)
            ?? throw new InvalidOperationException($"Unable to deserialize content directory '{directoryPath}'.");
        return package;
    }

    private static JsonArray LoadDefinitionArray(string directoryPath, ContentTypeSpec spec)
    {
        var definitions = new JsonArray();
        foreach (var entry in DefinitionSourceReader.Read(directoryPath, spec, required: true))
        {
            definitions.Add(entry.Definition);
        }

        return definitions;
    }

    private static Dictionary<string, StoryScript> LoadStoryScripts(string directoryPath)
    {
        var scripts = new Dictionary<string, StoryScript>(StringComparer.Ordinal);
        foreach (var entry in StorySourceReader.Read(directoryPath))
        {
            Ensure(scripts.TryAdd(entry.ScriptId, entry.Script),
                $"Story script '{entry.ScriptId}' is duplicated.");
        }

        return scripts;
    }
}
