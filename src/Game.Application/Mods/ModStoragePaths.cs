namespace Game.Application.Mods;

public sealed record ModStoragePaths(string RootDirectoryPath, string ModId)
{
    public string ModUserDataDirectoryPath => Path.Combine(RootDirectoryPath, ProjectDataRoot.UserDataDirectoryName, ModId);
    public string SaveDirectoryPath => Path.Combine(ModUserDataDirectoryPath, "saves");
    public string AutoSavePath => Path.Combine(SaveDirectoryPath, "autosave.json");
    public string QuickSavePath => Path.Combine(SaveDirectoryPath, "quicksave.json");
    public string ProfilePath => Path.Combine(ModUserDataDirectoryPath, "profile.json");
    public string SettingsPath => Path.Combine(ModUserDataDirectoryPath, "settings.json");

    public string GetSaveSlotPath(int slotIndex) =>
        Path.Combine(SaveDirectoryPath, $"save-slot-{slotIndex}.json");
}
