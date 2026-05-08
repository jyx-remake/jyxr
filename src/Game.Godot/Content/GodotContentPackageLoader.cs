using System.Text.Json;
using Game.Content.Loading;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Serialization;
using Game.Core.Story;
using GodotDirAccess = Godot.DirAccess;
using GodotFileAccess = Godot.FileAccess;

namespace Game.Godot;

public sealed class GodotContentPackageLoader
{
	private const string BattlesFileName = "battles.json";
	private const string CharactersFileName = "characters.json";
	private const string ExternalSkillsFileName = "external-skills.json";
	private const string GameTipsFileName = "game-tips.json";
	private const string GrowTemplatesFileName = "grow-templates.json";
	private const string InternalSkillsFileName = "internal-skills.json";
	private const string LegendSkillsFileName = "legend-skills.json";
	private const string MapsFileName = "maps.json";
	private const string WorldTriggersFileName = "world-triggers.json";
	private const string ResourcesFileName = "resources.json";
	private const string SectsFileName = "sects.json";
	private const string ShopsFileName = "shops.json";
	private const string SpecialSkillsFileName = "special-skills.json";
	private const string StoryDirectoryName = "story";
	private const string StoryFileSuffix = ".story.json";
	private const string ItemsFileName = "items.json";
	private const string BuffsFileName = "buffs.json";
	private const string TalentsFileName = "talents.json";
	private const string TowersFileName = "towers.json";

	private readonly string _rootPath;

	public GodotContentPackageLoader(string rootPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
		_rootPath = NormalizePath(rootPath);
	}

	public ContentPackage Load()
	{
		if (GodotDirAccess.Open(_rootPath) is null)
		{
			throw new DirectoryNotFoundException($"Content directory '{_rootPath}' was not found.");
		}

		return new ContentPackage
		{
			Battles = LoadRequiredList<BattleDefinition>(BattlesFileName),
			Characters = LoadRequiredList<CharacterDefinition>(CharactersFileName),
			ExternalSkills = LoadRequiredList<ExternalSkillDefinition>(ExternalSkillsFileName),
			GameTips = LoadRequiredList<GameTipDefinition>(GameTipsFileName),
			GrowTemplates = LoadRequiredList<GrowTemplateDefinition>(GrowTemplatesFileName),
			InternalSkills = LoadRequiredList<InternalSkillDefinition>(InternalSkillsFileName),
			LegendSkills = LoadRequiredList<LegendSkillDefinition>(LegendSkillsFileName),
			Maps = LoadRequiredList<MapDefinition>(MapsFileName),
			WorldTriggers = LoadRequiredList<WorldTriggerDefinition>(WorldTriggersFileName),
			Resources = LoadRequiredList<ResourceDefinition>(ResourcesFileName),
			Sects = LoadRequiredList<SectDefinition>(SectsFileName),
			Shops = LoadRequiredList<ShopDefinition>(ShopsFileName),
			SpecialSkills = LoadRequiredList<SpecialSkillDefinition>(SpecialSkillsFileName),
			StoryScripts = LoadStoryScripts(),
			Items = LoadRequiredList<ItemDefinition>(ItemsFileName),
			Buffs = LoadRequiredList<BuffDefinition>(BuffsFileName),
			Talents = LoadRequiredList<TalentDefinition>(TalentsFileName),
			Towers = LoadRequiredList<TowerDefinition>(TowersFileName),
		};
	}

	public string ReadText(string relativePath)
	{
		var path = ResolvePath(relativePath);
		if (!GodotFileAccess.FileExists(path))
		{
			throw new FileNotFoundException($"Content file '{relativePath}' was not found in '{_rootPath}'.", path);
		}

		using var file = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Read);
		if (file is null)
		{
			throw new FileNotFoundException($"Content file '{relativePath}' was not found in '{_rootPath}'.", path);
		}

		return file.GetAsText();
	}

	private List<T> LoadRequiredList<T>(string fileName)
	{
		var json = ReadText(fileName);
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

	private Dictionary<string, StoryScript> LoadStoryScripts()
	{
		var storyDirectoryPath = ResolvePath(StoryDirectoryName);
		if (GodotDirAccess.Open(storyDirectoryPath) is null)
		{
			return new Dictionary<string, StoryScript>(StringComparer.Ordinal);
		}

		var scripts = new Dictionary<string, StoryScript>(StringComparer.Ordinal);
		foreach (var storyPath in EnumerateFiles(StoryDirectoryName))
		{
			if (!storyPath.EndsWith(StoryFileSuffix, StringComparison.Ordinal))
			{
				continue;
			}

			var scriptId = BuildStoryScriptId(storyPath);
			if (!scripts.TryAdd(scriptId, StoryScriptJson.Parse(ReadText(storyPath))))
			{
				throw new InvalidOperationException($"Story script '{scriptId}' is duplicated.");
			}
		}

		return scripts;
	}

	private IEnumerable<string> EnumerateFiles(string relativeDirectoryPath)
	{
		var normalizedDirectoryPath = NormalizePath(relativeDirectoryPath);
		using var directory = GodotDirAccess.Open(ResolvePath(normalizedDirectoryPath));
		if (directory is null)
		{
			yield break;
		}

		directory.ListDirBegin();
		try
		{
			while (true)
			{
				var entry = directory.GetNext();
				if (string.IsNullOrEmpty(entry))
				{
					yield break;
				}

				if (entry is "." or "..")
				{
					continue;
				}

				var childRelativePath = CombinePath(normalizedDirectoryPath, entry);
				if (directory.CurrentIsDir())
				{
					foreach (var nestedPath in EnumerateFiles(childRelativePath))
					{
						yield return nestedPath;
					}

					continue;
				}

				yield return childRelativePath;
			}
		}
		finally
		{
			directory.ListDirEnd();
		}
	}

	private static string BuildStoryScriptId(string storyPath)
	{
		var normalizedStoryPath = NormalizePath(storyPath);
		var prefix = $"{StoryDirectoryName}/";
		if (!normalizedStoryPath.StartsWith(prefix, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				$"Story file '{normalizedStoryPath}' must be under '{StoryDirectoryName}'.");
		}

		var relativePath = normalizedStoryPath[prefix.Length..];
		if (!relativePath.EndsWith(StoryFileSuffix, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				$"Story file '{relativePath}' must end with '{StoryFileSuffix}'.");
		}

		return relativePath[..^StoryFileSuffix.Length];
	}

	private string ResolvePath(string relativePath)
	{
		var normalizedPath = NormalizePath(relativePath);
		return string.IsNullOrEmpty(normalizedPath)
			? _rootPath
			: $"{_rootPath}/{normalizedPath}";
	}

	private static string CombinePath(string left, string right)
	{
		var normalizedLeft = NormalizePath(left);
		var normalizedRight = NormalizePath(right);
		if (string.IsNullOrEmpty(normalizedLeft))
		{
			return normalizedRight;
		}

		if (string.IsNullOrEmpty(normalizedRight))
		{
			return normalizedLeft;
		}

		return $"{normalizedLeft}/{normalizedRight}";
	}

	private static string NormalizePath(string path) =>
		string.IsNullOrWhiteSpace(path)
			? string.Empty
			: path.Replace('\\', '/').Trim('/');
}
