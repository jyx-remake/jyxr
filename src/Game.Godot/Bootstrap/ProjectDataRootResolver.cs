using Game.Application.Mods;
using Godot;

namespace Game.Godot;

internal static class ProjectDataRootResolver
{
    public static ProjectDataRoot Resolve()
    {
        if (OS.HasFeature("editor"))
        {
            // The Godot host lives at <repository>/src/Game.Godot.
            return ProjectDataRoot.FromPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../.."));
        }

        if (OS.HasFeature("android") || OS.HasFeature("web_android"))
        {
            return ProjectDataRoot.FromPath("/storage/emulated/0/JYXR");
        }

        return ProjectDataRoot.FromPath(Path.GetDirectoryName(OS.GetExecutablePath()) ?? OS.GetUserDataDir());
    }
}
