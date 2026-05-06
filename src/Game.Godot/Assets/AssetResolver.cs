using Game.Content.Loading;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model.Character;
using Godot;
using System.IO;

namespace Game.Godot.Assets;

public static class AssetResolver
{
	private const string AssetsDirectoryPath = "res://assets";
	private const string ArtDirectoryPath = "res://assets/art";
	private const string AnimationDirectoryPath = "res://assets/animation";
	private const string IconDirectoryPath = "res://assets/art/icon";
	private const string AudioDirectoryPath = "res://assets/audio";

	private static readonly string[] TextureExtensions = [".png", ".jpg", ".jpeg", ".webp"];
	private static readonly string[] AudioExtensions = [".ogg", ".mp3", ".wav", ".flac"];

	public static Texture2D? LoadTextureResource(string? resourceId) =>
		LoadAsset<Texture2D>(ResolveAssetPath(resourceId, ArtDirectoryPath, TextureExtensions, "Texture"), "Texture");

	public static Texture2D? LoadSkillIconResource(string? resourceId)
	{
		var texture = LoadAsset<Texture2D>(ResolveConventionalTexturePath(resourceId, IconDirectoryPath), "Texture");
		if (texture is not null)
		{
			return texture;
		}

		return LoadTextureResource(resourceId);
	}

	public static Texture2D? LoadBattleBackgroundResource(string? mapId) =>
		LoadAsset<Texture2D>(ResolveConventionalTexturePath(mapId, $"{ArtDirectoryPath}/battle_bg"), "Texture");

	public static AudioStream? LoadAudioResource(string? resourceId) =>
		LoadAsset<AudioStream>(ResolveAssetPath(resourceId, AudioDirectoryPath, AudioExtensions, "Audio"), "Audio");

	public static Texture2D? LoadCharacterPortrait(string? portrait) =>
		LoadTextureResource(portrait);

	public static Texture2D? LoadCharacterPortrait(CharacterDefinition definition)
	{
		ArgumentNullException.ThrowIfNull(definition);
		return LoadCharacterPortrait(definition.Portrait);
	}

	public static Texture2D? LoadCharacterPortraitByCharacterId(string? characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return null;
		}

		return TryGetCharacterById(characterId.Trim(), out var definition)
			? LoadCharacterPortrait(definition)
			: null;
	}

	public static Texture2D? LoadCharacterPortrait(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		return LoadCharacterPortrait(character.Portrait);
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
		if (Game.PartyService.TryFindAllMember(characterId, out var character))
		{
			return character.Name;
		}

		if (Game.ContentRepository.TryGetCharacter(characterId, out var definition))
		{
			return definition.Name;
		}

		return characterId;
	}

	public static (string DisplayName, Texture2D? Portrait) ResolveSpeakerPresentation(string? speaker)
	{
		var normalizedSpeaker = speaker?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalizedSpeaker))
		{
			return (string.Empty, null);
		}

		if (Game.PartyService.TryFindAllMember(normalizedSpeaker, out var character))
		{
			return (character.Name, LoadCharacterPortrait(character));
		}

		if (TryGetCharacterByIdOrName(normalizedSpeaker, out var definition))
		{
			return (definition.Name, LoadCharacterPortrait(definition));
		}

		return (normalizedSpeaker, null);
	}

	private static T? LoadAsset<T>(string? resourcePath, string assetKind)
		where T : Resource
	{
		if (resourcePath is null)
		{
			return null;
		}

		if (!ResourceLoader.Exists(resourcePath))
		{
			Game.Logger.Warning($"{assetKind} resource does not exist: {resourcePath}");
			return null;
		}

		return ResourceLoader.Load<T>(resourcePath);
	}

	private static string? ResolveAssetPath(
		string? resourceId,
		string baseDirectoryPath,
		IReadOnlyList<string> extensions,
		string assetKind)
	{
		if (string.IsNullOrWhiteSpace(resourceId))
		{
			return null;
		}

		var normalizedResourceId = resourceId.Trim();
		if (normalizedResourceId.StartsWith("res://", StringComparison.Ordinal))
		{
			return ResolveExistingPath(normalizedResourceId, extensions);
		}

		var resource = TryGetResource(normalizedResourceId, assetKind);
		if (resource is null)
		{
			return null;
		}

		var relativePath = NormalizeResourceValue(resource.Value, normalizedResourceId, assetKind);
		return relativePath is null
			? null
			: ResolveExistingPath(BuildAssetPath(baseDirectoryPath, relativePath), extensions);
	}

	private static ResourceDefinition? TryGetResource(string resourceId, string assetKind)
	{
		if (Game.ContentRepository.TryGetResource(resourceId, out var resource))
		{
			return resource;
		}

		Game.Logger.Warning($"{assetKind} resource is missing: {resourceId}");
		return null;
	}

	private static string? NormalizeResourceValue(string value, string resourceId, string assetKind)
	{
		var relativePath = value.Trim().Replace('\\', '/');
		if (!string.IsNullOrWhiteSpace(relativePath))
		{
			return relativePath;
		}

		Game.Logger.Warning($"{assetKind} resource value is empty: {resourceId}");
		return null;
	}

	private static string BuildAssetPath(string baseDirectoryPath, string relativePath)
	{
		if (relativePath.StartsWith("res://", StringComparison.Ordinal))
		{
			return relativePath;
		}

		if (relativePath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
		{
			return $"res://{relativePath}";
		}

		if (relativePath.StartsWith("art/", StringComparison.OrdinalIgnoreCase) ||
			relativePath.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
		{
			return $"{AssetsDirectoryPath}/{relativePath}";
		}

		return $"{baseDirectoryPath}/{relativePath}";
	}

	private static string ResolveExistingPath(string path, IReadOnlyList<string> extensions)
	{
		if (!Path.HasExtension(path))
		{
			return ResolveExtensionlessPath(path, extensions) ?? $"{path}{extensions[0]}";
		}

		if (ResourceLoader.Exists(path))
		{
			return path;
		}

		var extensionlessPath = Path.ChangeExtension(path, null)?.Replace('\\', '/').TrimEnd('.');
		return string.IsNullOrWhiteSpace(extensionlessPath)
			? path
			: ResolveExtensionlessPath(extensionlessPath, extensions) ?? path;
	}

	private static string? ResolveExtensionlessPath(string path, IReadOnlyList<string> extensions)
	{
		foreach (var extension in extensions)
		{
			var candidate = $"{path}{extension}";
			if (ResourceLoader.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	private static string? ResolveConventionalTexturePath(string? resourceId, string baseDirectoryPath)
	{
		if (string.IsNullOrWhiteSpace(resourceId))
		{
			return null;
		}

		var normalizedResourceId = resourceId.Trim();
		if (normalizedResourceId.StartsWith("res://", StringComparison.Ordinal))
		{
			return ResolveExistingPath(normalizedResourceId, TextureExtensions);
		}

		return ResolveExistingPath($"{baseDirectoryPath}/{normalizedResourceId}", TextureExtensions);
	}

	private static AnimationLibrary? LoadAnimationLibrary(string? resourceId, string category)
	{
		if (string.IsNullOrWhiteSpace(resourceId))
		{
			return null;
		}

		var normalizedResourceId = resourceId.Trim();
		var resourcePath = normalizedResourceId.StartsWith("res://", StringComparison.Ordinal)
			? ResolveExistingPath(normalizedResourceId, [".tres", ".res"])
			: ResolveExistingPath($"{AnimationDirectoryPath}/{category}/{normalizedResourceId}", [".tres", ".res"]);
		return LoadAsset<AnimationLibrary>(resourcePath, "AnimationLibrary");
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
