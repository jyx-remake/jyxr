using Game.Application;
using Game.Content.Loading;
using Game.Core.Definitions;
using Game.Core.Model.Character;
using Godot;
using System.IO;

namespace Game.Godot.Assets;

public static class AssetResolver
{
	private const string AssetsDirectoryPath = "res://assets";
	private const string AnimationDirectoryPath = "res://assets/animation";
	private static readonly IReadOnlyDictionary<string, string> LegacyResourceDirectories =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["head"] = "Heads",
			["item"] = "Items",
			["map"] = "Maps",
			["audio"] = "Audios",
			["ui"] = "UI",
			["icon"] = "Icons",
			["cg"] = "CGs",
			["mv"] = "Movies",
			["video"] = "Movies",
			["battle_bg"] = "BattleBg",
			["battlebg"] = "BattleBg",
		};
	private static readonly IReadOnlyDictionary<string, string> NavigationPortraits =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["返回"] = "UI.back",
			["向前走"] = "UI.front",
			["向左走"] = "UI.left",
			["向右走"] = "UI.right",
		};

	public static Texture2D? LoadTexture(string? reference) =>
		LoadMedia<Texture2D>(reference, MediaAssetKind.Texture);

	public static AudioStream? LoadAudio(string? reference) =>
		LoadMedia<AudioStream>(reference, MediaAssetKind.Audio);

	public static VideoStream? LoadVideo(string? reference) =>
		LoadMedia<VideoStream>(reference, MediaAssetKind.Video);

	public static string? ResolveCharacterPortraitReferenceByCharacterId(string? characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return null;
		}

		var normalized = characterId.Trim();
		if (TryGetCharacterById(normalized, out var definition) &&
			!string.IsNullOrWhiteSpace(definition.Portrait))
		{
			return definition.Portrait;
		}

		if (NavigationPortraits.TryGetValue(normalized, out var navigationPortrait))
		{
			return navigationPortrait;
		}

		// Duplicate map units disambiguate a repeated name with a "~N"
		// occurrence suffix (for example 剑侍 -> 剑侍~2).  Resolve the base
		// name so a repeating character still resolves its own portrait.
		var baseId = StripMapUnitNameSuffix(normalized);
		if (!string.Equals(baseId, normalized, StringComparison.Ordinal))
		{
			if (TryGetCharacterById(baseId, out definition) &&
				!string.IsNullOrWhiteSpace(definition.Portrait))
			{
				return definition.Portrait;
			}

			if (NavigationPortraits.TryGetValue(baseId, out navigationPortrait))
			{
				return navigationPortrait;
			}
		}

		return null;
	}

	public static string? ResolveCharacterModelId(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		return character.ResolvedModelId ?? character.Model ?? character.Definition.Model;
	}

	public static AnimationLibrary? LoadCombatantAnimation(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		return LoadCombatantAnimation(ResolveCharacterModelId(character));
	}

	public static AnimationLibrary? LoadCombatantAnimation(string? modelId) =>
		LoadAnimationLibrary(modelId, "combatant");

	public static AnimationLibrary? LoadSkillAnimation(string? animationId) =>
		LoadAnimationLibrary(animationId, "skill");

	public static string ResolveCharacterName(string characterId)
	{
		if (TryResolveCharacterNameFromId(characterId, out var resolvedName))
		{
			return resolvedName;
		}

		// Duplicate map units disambiguate a repeated name with a "~N"
		// occurrence suffix (for example 剑侍 -> 剑侍~2).  Fall back to the
		// base name so a repeating character shows its real display name.
		var baseId = StripMapUnitNameSuffix(characterId);
		if (TryResolveCharacterNameFromId(baseId, out resolvedName))
		{
			return resolvedName;
		}

		return baseId;
	}

	private static bool TryResolveCharacterNameFromId(string characterId, out string resolvedName)
	{
		if (Game.PartyService.TryFindAllMember(characterId, out var character))
		{
			resolvedName = character.Name;
			return true;
		}

		if (Game.ContentRepository.TryGetCharacter(characterId, out var definition))
		{
			resolvedName = definition.Name;
			return true;
		}

		resolvedName = characterId;
		return false;
	}

	public static (string DisplayName, Texture2D? Portrait) ResolveSpeakerPresentation(
		string? speaker,
		string? portraitOverride = null)
	{
		var normalizedSpeaker = speaker?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalizedSpeaker))
		{
			return (string.Empty, LoadTexture(portraitOverride));
		}

		if (Game.PartyService.TryFindAllMember(normalizedSpeaker, out var character))
		{
			return (character.Name, LoadTexture(portraitOverride ?? character.Portrait));
		}

		if (TryGetCharacterByIdOrName(normalizedSpeaker, out var definition))
		{
			return (definition.Name, LoadTexture(portraitOverride ?? definition.Portrait));
		}

		return (normalizedSpeaker, LoadTexture(portraitOverride));
	}

	private static T? LoadMedia<T>(
		string? reference,
		MediaAssetKind assetKind)
		where T : Resource
	{
		if (string.IsNullOrWhiteSpace(reference))
		{
			return null;
		}

		var resolution = MediaReferenceResolver.Resolve(reference, assetKind, Game.ContentRepository);
		if (!resolution.IsSuccess)
		{
			Game.Logger.Warning(
				$"{assetKind} reference could not be resolved: '{reference}'. {resolution.Error}");
			return null;
		}

		var candidatePaths = GetCandidateMediaPaths(resolution.AssetPath!, assetKind).ToArray();
		var resourcePath = candidatePaths.FirstOrDefault(static path => ResourceLoader.Exists(path));
		if (resourcePath is null)
		{
			Game.Logger.Warning(
				$"{assetKind} {resolution.ReferenceKind} reference '{reference}' does not exist. Candidate paths: {string.Join(", ", candidatePaths)}");
			return null;
		}

		var resource = ResourceLoader.Load<T>(resourcePath);
		if (resource is null)
		{
			Game.Logger.Warning(
				$"{assetKind} {resolution.ReferenceKind} reference '{reference}' could not be loaded as {typeof(T).Name}. Resolved path: {resourcePath}");
		}

		return resource;
	}

	private static IEnumerable<string> GetCandidateMediaPaths(string assetPath, MediaAssetKind assetKind)
	{
		var candidates = MediaReferenceResolver
			.GetCandidateAssetPaths(assetPath, assetKind)
			.ToArray();

		// The legacy XMJH PCK keeps resources in their original top-level folders
		// (Heads, Items, Maps, Audios, ...), while the content manifest uses the
		// engine-neutral paths (art/head, art/item, audio, ...). Probe active mods
		// first so an addon can override a primary-mod asset, then fall back to the
		// built-in engine assets for resources that XMJH does not ship.
		var legacyPath = assetKind == MediaAssetKind.Texture && assetPath.StartsWith("art/", StringComparison.OrdinalIgnoreCase)
			? assetPath["art/".Length..]
			: assetPath;
		var segments = legacyPath.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
		if (Game.IsInitialized &&
			segments.Length == 2 && LegacyResourceDirectories.TryGetValue(segments[0], out var legacyDirectory))
		{
			var relativePath = segments[1];
			foreach (var mod in Game.ActiveModLoadout.ModsInLoadOrder.Reverse())
			{
				foreach (var candidate in MediaReferenceResolver.GetCandidateAssetPaths(relativePath, assetKind))
				{
					yield return $"res://mods/{mod.ModId}/resources/legacy/{legacyDirectory}/{candidate}";
				}
			}
		}

		foreach (var candidate in candidates)
		{
			yield return $"{AssetsDirectoryPath}/{candidate}";
		}
	}

	private static string? ResolveAnimationPath(string path)
	{
		if (Path.HasExtension(path))
		{
			return ResourceLoader.Exists(path) ? path : null;
		}

		foreach (var extension in new[] { ".tres", ".res" })
		{
			var candidate = $"{path}{extension}";
			if (ResourceLoader.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	private static AnimationLibrary? LoadAnimationLibrary(string? resourceId, string category)
	{
		if (string.IsNullOrWhiteSpace(resourceId))
		{
			return null;
		}

		var normalizedResourceId = resourceId.Trim();
		var resourcePath = normalizedResourceId.StartsWith("res://", StringComparison.Ordinal)
			? ResolveAnimationPath(normalizedResourceId)
			: GetCandidateAnimationPaths(normalizedResourceId, category)
				.Select(ResolveAnimationPath)
				.FirstOrDefault(static path => path is not null);
		if (resourcePath is null)
		{
			Game.Logger.Warning($"AnimationLibrary resource does not exist: {normalizedResourceId}");
			return null;
		}

		return ResourceLoader.Load<AnimationLibrary>(resourcePath);
	}

	private static IEnumerable<string> GetCandidateAnimationPaths(string resourceId, string category)
	{
		// Animation libraries obey the same override direction as media assets:
		// the last addon wins, then the primary mod, and built-in engine assets
		// are only a fallback.  A standalone game mod can therefore package its
		// complete animation closure without a hard-coded dependency on XMJH or
		// jyxr-base.
		if (Game.IsInitialized)
		{
			foreach (var mod in Game.ActiveModLoadout.ModsInLoadOrder.Reverse())
			{
				yield return $"res://mods/{mod.ModId}/resources/converted/AnimationLibraries/{category}/{resourceId}";
			}
		}

		yield return $"{AnimationDirectoryPath}/{category}/{resourceId}";
	}

	private static bool TryGetCharacterById(string characterId, out CharacterDefinition definition)
	{
		if (Game.ContentRepository.TryGetCharacter(characterId, out var resolvedDefinition))
		{
			definition = resolvedDefinition;
			return true;
		}

		definition = null!;
		return false;
	}

	private static string StripMapUnitNameSuffix(string name)
	{
		var value = name.Trim();
		if (value.Length == 0)
		{
			return value;
		}

		var lastTilde = value.LastIndexOf('~');
		if (lastTilde < 0 || lastTilde + 1 >= value.Length)
		{
			return value;
		}

		var suffix = value[(lastTilde + 1)..];
		if (suffix.Length == 0 || !suffix.All(static c => c is >= '0' and <= '9'))
		{
			return value;
		}

		return value[..lastTilde];
	}

	private static bool TryGetCharacterByIdOrName(string idOrName, out CharacterDefinition definition)
	{
		if (TryGetCharacterById(idOrName, out definition))
		{
			return true;
		}

		if (Game.ContentRepository is InMemoryContentRepository repository)
		{
			foreach (var candidate in repository.Characters.Values)
			{
				if (!string.Equals(candidate.Name, idOrName, StringComparison.Ordinal))
				{
					continue;
				}

				definition = candidate;
				return true;
			}
		}

		definition = null!;
		return false;
	}
}
