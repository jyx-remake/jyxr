using System.Text.Json;
using Game.Application;
using Game.Core.Serialization;

namespace Game.Godot.Persistence;

public sealed class LocalUserSettingsStore
{
	private readonly string? _settingsPath;
	private readonly IDiagnosticLogger? _logger;

	public LocalUserSettingsStore(string? settingsPath = null, IDiagnosticLogger? logger = null)
	{
		_settingsPath = string.IsNullOrWhiteSpace(settingsPath) ? null : Path.GetFullPath(settingsPath.Trim());
		_logger = logger;
	}

	public string Save(UserSettingsRecord settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		var absolutePath = ResolveAbsolutePath();
		var directoryPath = Path.GetDirectoryName(absolutePath);
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			throw new InvalidOperationException($"Invalid settings path: {_settingsPath}");
		}

		Directory.CreateDirectory(directoryPath);
		var json = JsonSerializer.Serialize(settings, GameJson.Default);
		File.WriteAllText(absolutePath, json);
		Logger.Info($"Saved user settings to '{absolutePath}'.");
		return absolutePath;
	}

	public bool TryLoad(out UserSettingsRecord? settings)
	{
		var absolutePath = ResolveAbsolutePath();
		if (!File.Exists(absolutePath))
		{
			settings = null;
			return false;
		}

		try
		{
			var json = File.ReadAllText(absolutePath);
			var loadedSettings = JsonSerializer.Deserialize<UserSettingsRecord>(json, GameJson.Default);
			if (loadedSettings is null)
			{
				Logger.Warning($"User settings could not be deserialized: {absolutePath}");
				settings = null;
				return false;
			}

			if (loadedSettings.Version != UserSettingsRecord.CurrentVersion)
			{
				Logger.Warning(
					$"User settings version mismatch: {loadedSettings.Version}, supported {UserSettingsRecord.CurrentVersion}. Falling back to defaults.");
				settings = null;
				return false;
			}

			settings = loadedSettings;
			Logger.Info($"Loaded user settings from '{absolutePath}'.");
			return true;
		}
		catch (Exception exception)
		{
			Logger.Warning($"User settings read failed: {absolutePath}. {exception.Message}");
			settings = null;
			return false;
		}
	}

	public UserSettingsRecord LoadOrDefault() =>
		TryLoad(out var settings) && settings is not null
			? settings
			: UserSettingsRecord.Default;

	private string ResolveAbsolutePath() => _settingsPath ?? Game.ActiveMod.StoragePaths.SettingsPath;

	private IDiagnosticLogger Logger => _logger ?? Game.Logger;
}
