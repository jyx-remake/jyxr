using System.Text.Json;
using Game.Core.Serialization;

namespace Game.Application.Mods;

public sealed record LauncherSettingsRecord(
    int Version,
    string? ActiveModId)
{
    public const int CurrentVersion = 2;

    public static LauncherSettingsRecord Empty => new(CurrentVersion, null);
}

public sealed class LauncherSettingsStore
{
    private readonly string _settingsPath;

    public LauncherSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    public LauncherSettingsRecord LoadOrEmpty()
    {
        if (!File.Exists(_settingsPath))
        {
            return LauncherSettingsRecord.Empty;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<LauncherSettingsRecord>(json, GameJson.Default);
            return settings is { Version: LauncherSettingsRecord.CurrentVersion }
                ? settings
                : LauncherSettingsRecord.Empty;
        }
        catch
        {
            return LauncherSettingsRecord.Empty;
        }
    }

    public void Save(LauncherSettingsRecord settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directoryPath = Path.GetDirectoryName(_settingsPath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"Invalid launcher settings path: {_settingsPath}");
        }

        Directory.CreateDirectory(directoryPath);
        var json = JsonSerializer.Serialize(settings, GameJson.Default);
        File.WriteAllText(_settingsPath, json);
    }
}
