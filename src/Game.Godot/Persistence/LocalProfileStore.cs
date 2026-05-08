using System.Text.Json;
using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;
using Godot;

namespace Game.Godot.Persistence;

public sealed class LocalProfileStore
{
	public const string DefaultProfilePath = "user://saves/profile.json";

	private readonly string _profilePath;

	public LocalProfileStore(string? profilePath = null)
	{
		_profilePath = string.IsNullOrWhiteSpace(profilePath)
			? DefaultProfilePath
			: profilePath.Trim();
	}

	public string SaveCurrentProfile()
	{
		var absolutePath = ResolveAbsolutePath();
		var directoryPath = Path.GetDirectoryName(absolutePath);
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			throw new InvalidOperationException($"Invalid profile path: {_profilePath}");
		}

		Directory.CreateDirectory(directoryPath);
		var json = JsonSerializer.Serialize(Game.ProfileService.CreateProfileRecord(), GameJson.Default);
		File.WriteAllText(absolutePath, json);
		Game.Logger.Info($"Saved global profile to '{absolutePath}'.");
		return absolutePath;
	}

	public bool TryLoad(out GameProfileRecord? profile)
	{
		var absolutePath = ResolveAbsolutePath();
		if (!File.Exists(absolutePath))
		{
			profile = null;
			return false;
		}

		try
		{
			var json = File.ReadAllText(absolutePath);
			var loadedProfile = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default);
			if (loadedProfile is null)
			{
				Game.Logger.Warning($"Global profile could not be deserialized: {absolutePath}");
				profile = null;
				return false;
			}

			if (loadedProfile.Version != GameProfileRecord.CurrentVersion)
			{
				Game.Logger.Warning(
					$"Global profile version mismatch: {loadedProfile.Version}, supported {GameProfileRecord.CurrentVersion}. Falling back to empty profile.");
				profile = null;
				return false;
			}

			Game.Logger.Info($"Loaded global profile from '{absolutePath}'.");
			profile = loadedProfile;
			return true;
		}
		catch (Exception exception)
		{
			Game.Logger.Warning($"Global profile read failed: {absolutePath}. {exception.Message}");
			profile = null;
			return false;
		}
	}

	public bool HasProfile() => File.Exists(ResolveAbsolutePath());

	public GameProfileRecord LoadOrEmpty() =>
		TryLoad(out var profile) && profile is not null
			? profile
			: GameProfileRecord.Create(new GameProfile());

	private string ResolveAbsolutePath() => ProjectSettings.GlobalizePath(_profilePath);
}
