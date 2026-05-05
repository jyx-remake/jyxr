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

	public GameProfileRecord Load()
	{
		var absolutePath = ResolveAbsolutePath();
		if (!File.Exists(absolutePath))
		{
			throw new InvalidOperationException($"未找到全局档案文件：{absolutePath}");
		}

		var json = File.ReadAllText(absolutePath);
		var profile = JsonSerializer.Deserialize<GameProfileRecord>(json, GameJson.Default)
			?? throw new InvalidOperationException("全局档案文件解析失败。");
		ValidateProfile(profile);
		Game.Logger.Info($"Loaded global profile from '{absolutePath}'.");
		return profile;
	}

	public bool HasProfile() => File.Exists(ResolveAbsolutePath());

	public GameProfileRecord LoadOrEmpty() =>
		HasProfile()
			? Load()
			: GameProfileRecord.Create(new GameProfile());

	private string ResolveAbsolutePath() => ProjectSettings.GlobalizePath(_profilePath);

	private static void ValidateProfile(GameProfileRecord profile)
	{
		if (profile.Version != GameProfileRecord.CurrentVersion)
		{
			throw new InvalidOperationException(
				$"全局档案版本不匹配：{profile.Version}，当前支持 {GameProfileRecord.CurrentVersion}。");
		}
	}
}
