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
	private const long DesktopTextureBudgetBytes = 128L * 1024 * 1024;
	private const long MobileTextureBudgetBytes = 64L * 1024 * 1024;
	private const int MaximumFailureEntries = 1024;
	private static ExternalTextureCatalog _externalTextures = new([]);
	private static TextureCache _textureCache = new(DesktopTextureBudgetBytes);
	private static readonly Dictionary<string, string> TextureSourcesByReference = new(StringComparer.Ordinal);
	private static readonly HashSet<(string Key, TextureFailureKind Kind)> TextureFailures = [];
	private static readonly Queue<(string Key, TextureFailureKind Kind)> RecentFailures = [];

	public static void InitializeTextures(IEnumerable<string> modDirectoriesInLoadOrder, long? cacheBudgetBytes = null)
	{
		var catalog = new ExternalTextureCatalog(modDirectoriesInLoadOrder);
		var defaultBudget = Game.IsMobilePlatform ? MobileTextureBudgetBytes : DesktopTextureBudgetBytes;
		var cache = new TextureCache(cacheBudgetBytes ?? defaultBudget);
		_externalTextures = catalog;
		_textureCache.Dispose();
		_textureCache = cache;
		TextureSourcesByReference.Clear();
		TextureFailures.Clear();
		RecentFailures.Clear();
		RegisterTextureCacheMonitors();
	}

	public static Texture2D? LoadTexture(string? reference)
	{
		if (string.IsNullOrWhiteSpace(reference))
		{
			return null;
		}

		var normalizedReference = reference.Trim();
		if (!TextureSourcesByReference.TryGetValue(normalizedReference, out var source))
		{
			if (TextureFailures.Contains((normalizedReference, TextureFailureKind.Reference)))
			{
				return null;
			}

			source = ResolveMediaPath(normalizedReference, MediaAssetKind.Texture);
			if (source is null)
			{
				RememberTextureFailure(normalizedReference, TextureFailureKind.Reference);
				return null;
			}

			TextureSourcesByReference.Add(normalizedReference, source);
		}

		if (TextureFailures.Contains((source, TextureFailureKind.Source)))
		{
			return null;
		}

		if (_textureCache.TryGet(source, out var cached))
		{
			return cached;
		}

		var isImported = source.StartsWith("res://", StringComparison.Ordinal);
		var texture = isImported
			? ResourceLoader.Load<Texture2D>(source)
			: LoadExternalTexture(source);
		if (texture is null)
		{
			RememberTextureFailure(source, TextureFailureKind.Source);
			Game.Logger.Warning($"Texture reference '{reference}' could not be loaded. Resolved path: {source}");
			return null;
		}

		_textureCache.Add(source, texture);
		return texture;
	}

	private static string? ResolveMediaPath(string reference, MediaAssetKind assetKind)
	{
		var resolution = MediaReferenceResolver.Resolve(reference, assetKind, Game.ContentRepository);
		if (!resolution.IsSuccess)
		{
			Game.Logger.Warning($"{assetKind} reference could not be resolved: '{reference}'. {resolution.Error}");
			return null;
		}

		var candidates = MediaReferenceResolver.GetCandidateAssetPaths(resolution.AssetPath!, assetKind);
		var externalPath = assetKind == MediaAssetKind.Texture ? _externalTextures.Resolve(candidates) : null;
		var source = externalPath ?? candidates
			.Select(path => $"{AssetsDirectoryPath}/{path}")
			.FirstOrDefault(static path => ResourceLoader.Exists(path));
		if (source is null)
		{
			Game.Logger.Warning($"{assetKind} reference '{reference}' does not exist. Candidate paths: {string.Join(", ", candidates)}");
		}

		return source;
	}

	private static void RememberTextureFailure(string key, TextureFailureKind kind)
	{
		var failure = (key, kind);
		if (!TextureFailures.Add(failure))
		{
			return;
		}

		RecentFailures.Enqueue(failure);
		while (RecentFailures.Count > MaximumFailureEntries)
		{
			TextureFailures.Remove(RecentFailures.Dequeue());
		}
	}

	private static void RegisterTextureCacheMonitors()
	{
		if (Performance.HasCustomMonitor("assets/texture_cache/retained_bytes"))
		{
			return;
		}

		Performance.AddCustomMonitor("assets/texture_cache/retained_bytes", Callable.From(() => _textureCache.RetainedBytes));
		Performance.AddCustomMonitor("assets/texture_cache/budget_bytes", Callable.From(() => _textureCache.BudgetBytes));
		Performance.AddCustomMonitor("assets/texture_cache/retained_groups", Callable.From(() => _textureCache.RetainedGroupCount));
		Performance.AddCustomMonitor("assets/texture_cache/sources", Callable.From(() => _textureCache.SourceCount));
		Performance.AddCustomMonitor("assets/texture_cache/hits", Callable.From(() => _textureCache.Hits));
		Performance.AddCustomMonitor("assets/texture_cache/weak_hits", Callable.From(() => _textureCache.WeakHits));
		Performance.AddCustomMonitor("assets/texture_cache/source_loads", Callable.From(() => _textureCache.Loads));
		Performance.AddCustomMonitor("assets/texture_cache/evictions", Callable.From(() => _textureCache.Evictions));
	}

	private static Texture2D? LoadExternalTexture(string path)
	{
		using var image = new Image();
		var error = image.Load(path);
		if (error != Error.Ok)
		{
			Game.Logger.Warning($"Failed to load external texture '{path}': {error}");
			return null;
		}

		image.FixAlphaEdges();
		return ImageTexture.CreateFromImage(image);
	}

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

		var resourcePath = ResolveMediaPath(reference, assetKind);
		if (resourcePath is null)
		{
			return null;
		}

		var resource = ResourceLoader.Load<T>(resourcePath);
		if (resource is null)
		{
			Game.Logger.Warning(
				$"{assetKind} reference '{reference}' could not be loaded as {typeof(T).Name}. Resolved path: {resourcePath}");
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

	private enum TextureFailureKind
	{
		Reference,
		Source,
	}
}
