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
	private static ExternalMediaCatalog _externalMedia = new([]);
	private static TextureCache _textureCache = new(DesktopTextureBudgetBytes);
	private static readonly Dictionary<(MediaAssetKind Kind, string Reference), string> SourcesByReference = [];
	private static readonly HashSet<(MediaAssetKind AssetKind, string Key, MediaFailureKind Kind)> MediaFailures = [];
	private static readonly Queue<(MediaAssetKind AssetKind, string Key, MediaFailureKind Kind)> RecentFailures = [];

	public static void InitializeMedia(IEnumerable<string> modDirectoriesInLoadOrder, long? cacheBudgetBytes = null)
	{
		var catalog = new ExternalMediaCatalog(modDirectoriesInLoadOrder);
		var defaultBudget = Game.IsMobilePlatform ? MobileTextureBudgetBytes : DesktopTextureBudgetBytes;
		var cache = new TextureCache(cacheBudgetBytes ?? defaultBudget);
		_externalMedia = catalog;
		_textureCache.Dispose();
		_textureCache = cache;
		SourcesByReference.Clear();
		MediaFailures.Clear();
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
		var source = ResolveMediaPath(normalizedReference, MediaAssetKind.Texture);
		if (source is null || MediaFailures.Contains((MediaAssetKind.Texture, source, MediaFailureKind.Source)))
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
			RememberMediaFailure(MediaAssetKind.Texture, source, MediaFailureKind.Source);
			Game.Logger.Warning($"Texture reference '{reference}' could not be loaded. Resolved path: {source}");
			return null;
		}

		_textureCache.Add(source, texture);
		return texture;
	}

	private static string? ResolveMediaPath(string reference, MediaAssetKind assetKind)
	{
		var key = (assetKind, reference);
		if (SourcesByReference.TryGetValue(key, out var cached))
		{
			return cached;
		}

		if (MediaFailures.Contains((assetKind, reference, MediaFailureKind.Reference)))
		{
			return null;
		}

		var resolution = MediaReferenceResolver.Resolve(reference, assetKind, Game.ContentRepository);
		if (!resolution.IsSuccess)
		{
			Game.Logger.Warning($"{assetKind} reference could not be resolved: '{reference}'. {resolution.Error}");
			RememberMediaFailure(assetKind, reference, MediaFailureKind.Reference);
			return null;
		}

		var candidates = MediaReferenceResolver.GetCandidateAssetPaths(resolution.AssetPath!, assetKind);
		var externalPath = _externalMedia.Resolve(candidates)?.Replace('\\', '/');
		var source = externalPath ?? candidates
			.Select(path => $"{AssetsDirectoryPath}/{path}")
			.FirstOrDefault(static path => ResourceLoader.Exists(path));
		if (source is null)
		{
			Game.Logger.Warning($"{assetKind} reference '{reference}' does not exist. Candidate paths: {string.Join(", ", candidates)}");
			RememberMediaFailure(assetKind, reference, MediaFailureKind.Reference);
			return null;
		}

		SourcesByReference.Add(key, source);
		return source;
	}

	private static void RememberMediaFailure(MediaAssetKind assetKind, string key, MediaFailureKind kind)
	{
		var failure = (assetKind, key, kind);
		if (!MediaFailures.Add(failure))
		{
			return;
		}

		RecentFailures.Enqueue(failure);
		while (RecentFailures.Count > MaximumFailureEntries)
		{
			MediaFailures.Remove(RecentFailures.Dequeue());
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

	public static AudioStream? LoadAudio(string? reference)
	{
		if (string.IsNullOrWhiteSpace(reference))
		{
			return null;
		}

		var source = ResolveMediaPath(reference.Trim(), MediaAssetKind.Audio);
		if (source is null || MediaFailures.Contains((MediaAssetKind.Audio, source, MediaFailureKind.Source)))
		{
			return null;
		}

		if (ResourceLoader.GetCachedRef(source) is AudioStream cached)
		{
			return cached;
		}

		var isImported = source.StartsWith("res://", StringComparison.Ordinal);
		var stream = isImported ? ResourceLoader.Load<AudioStream>(source) : LoadExternalAudio(source);
		if (stream is null)
		{
			RememberMediaFailure(MediaAssetKind.Audio, source, MediaFailureKind.Source);
			Game.Logger.Warning($"Audio reference '{reference}' could not be loaded. Resolved path: {source}");
			return null;
		}

		if (!isImported)
		{
			stream.TakeOverPath(source);
		}

		return stream;
	}

	private static AudioStream? LoadExternalAudio(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".ogg" => AudioStreamOggVorbis.LoadFromFile(path),
		".mp3" => AudioStreamMP3.LoadFromFile(path),
		".wav" => AudioStreamWav.LoadFromFile(path),
		_ => null,
	};

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
		if (resourcePath is null || MediaFailures.Contains((assetKind, resourcePath, MediaFailureKind.Source)))
		{
			return null;
		}

		var resource = ResourceLoader.Load<T>(resourcePath);
		if (resource is null)
		{
			RememberMediaFailure(assetKind, resourcePath, MediaFailureKind.Source);
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

	private enum MediaFailureKind
	{
		Reference,
		Source,
	}
}
