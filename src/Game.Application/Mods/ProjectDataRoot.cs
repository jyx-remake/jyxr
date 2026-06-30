namespace Game.Application.Mods;

public sealed record ProjectDataRoot(string Path)
{
    public const string UserDataDirectoryName = "userdata";

    public string ModsDirectoryPath => System.IO.Path.Combine(Path, "mods");
    public string LauncherDirectoryPath => System.IO.Path.Combine(Path, "launcher");
    public string LauncherSettingsPath => System.IO.Path.Combine(LauncherDirectoryPath, "settings.json");
    public string UserDataDirectoryPath => System.IO.Path.Combine(Path, UserDataDirectoryName);

    public static ProjectDataRoot FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new ProjectDataRoot(System.IO.Path.GetFullPath(path.Trim()));
    }
}
