using Game.Application;
using Game.Content.Loading;
using Game.Core.Definitions;
using Game.Core.Model.Character;
using Godot;
using System.IO;

namespace Game.Godot.Resources;

public static class AssetResolver
{
	private const string AssetsDirectoryPath = "res://assets";
	private const string AnimationDirectoryPath = "res://assets/animation";

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

		return TryGetCharacterById(characterId.Trim(), out var definition)
			? definition.Portrait
			: null;
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
			return (character.Name, LoadTexture(character.Portrait));
		}

		if (TryGetCharacterByIdOrName(normalizedSpeaker, out var definition))
		{
			return (definition.Name, LoadTexture(definition.Portrait));
		}

		return (normalizedSpeaker, null);
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

		var candidatePaths = MediaReferenceResolver
			.GetCandidateAssetPaths(resolution.AssetPath!, assetKind)
			.Select(path => $"{AssetsDirectoryPath}/{path}")
			.ToArray();
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
			: ResolveAnimationPath($"{AnimationDirectoryPath}/{category}/{normalizedResourceId}");
		if (resourcePath is null)
		{
			Game.Logger.Warning($"AnimationLibrary resource does not exist: {normalizedResourceId}");
			return null;
		}

		return ResourceLoader.Load<AnimationLibrary>(resourcePath);
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
