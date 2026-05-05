using System.Text.Json;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Serialization;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    private const string BattlesFileName = "battles.json";
    private const string CharactersFileName = "characters.json";
    private const string ExternalSkillsFileName = "external-skills.json";
    private const string GameTipsFileName = "game-tips.json";
    private const string GrowTemplatesFileName = "grow-templates.json";
    private const string InternalSkillsFileName = "internal-skills.json";
    private const string LegendSkillsFileName = "legend-skills.json";
    private const string MapsFileName = "maps.json";
    private const string ResourcesFileName = "resources.json";
    private const string SectsFileName = "sects.json";
    private const string ShopsFileName = "shops.json";
    private const string SpecialSkillsFileName = "special-skills.json";
    private const string StoryDirectoryName = "story";
    private const string StoryFilePattern = "*.story.json";
    private const string ItemsFileName = "items.json";
    private const string BuffsFileName = "buffs.json";
    private const string TalentsFileName = "talents.json";
    private const string TowersFileName = "towers.json";

    private static ContentPackage LoadPackageFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Content directory '{directoryPath}' was not found.");
        }

        return new ContentPackage
        {
            Battles = LoadRequiredList<BattleDefinition>(directoryPath, BattlesFileName),
            Characters = LoadRequiredList<CharacterDefinition>(directoryPath, CharactersFileName),
            ExternalSkills = LoadRequiredList<ExternalSkillDefinition>(directoryPath, ExternalSkillsFileName),
            GameTips = LoadRequiredList<GameTipDefinition>(directoryPath, GameTipsFileName),
            GrowTemplates = LoadRequiredList<GrowTemplateDefinition>(directoryPath, GrowTemplatesFileName),
            InternalSkills = LoadRequiredList<InternalSkillDefinition>(directoryPath, InternalSkillsFileName),
            LegendSkills = LoadRequiredList<LegendSkillDefinition>(directoryPath, LegendSkillsFileName),
            Maps = LoadRequiredList<MapDefinition>(directoryPath, MapsFileName),
            Resources = LoadRequiredList<ResourceDefinition>(directoryPath, ResourcesFileName),
            Sects = LoadRequiredList<SectDefinition>(directoryPath, SectsFileName),
            Shops = LoadRequiredList<ShopDefinition>(directoryPath, ShopsFileName),
            SpecialSkills = LoadRequiredList<SpecialSkillDefinition>(directoryPath, SpecialSkillsFileName),
            StoryScripts = LoadStoryScripts(directoryPath),
            Items = LoadRequiredList<ItemDefinition>(directoryPath, ItemsFileName),
            Buffs = LoadRequiredList<BuffDefinition>(directoryPath, BuffsFileName),
            Talents = LoadRequiredList<TalentDefinition>(directoryPath, TalentsFileName),
            Towers = LoadRequiredList<TowerDefinition>(directoryPath, TowersFileName),
        };
    }

    private static List<T> LoadRequiredList<T>(string directoryPath, string fileName)
    {
        var filePath = Path.Combine(directoryPath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Content file '{fileName}' was not found in '{directoryPath}'.", filePath);
        }

        var json = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Array => JsonSerializer.Deserialize<List<T>>(json, GameJson.Default)
                ?? throw new InvalidOperationException($"Unable to deserialize content file '{fileName}'."),
            JsonValueKind.Object => [JsonSerializer.Deserialize<T>(json, GameJson.Default)
                ?? throw new InvalidOperationException($"Unable to deserialize content file '{fileName}'.")],
            _ => throw new InvalidOperationException($"Content file '{fileName}' must be a JSON object or array."),
        };
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
