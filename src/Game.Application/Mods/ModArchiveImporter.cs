using System.IO.Compression;

namespace Game.Application.Mods;

public sealed class ModArchiveImporter(ProjectDataRoot projectDataRoot)
{
    public IReadOnlyList<string> Import(Stream source)
    {
        var workspace = Path.Combine(projectDataRoot.LauncherDirectoryPath, $"import-{Guid.NewGuid():N}");
        var payload = Path.Combine(workspace, "payload");
        var backup = Path.Combine(workspace, "backup");
        Directory.CreateDirectory(payload);
        var cleanWorkspace = true;
        try
        {
            using (var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true))
            {
                Extract(archive, payload);
            }

            var mods = LoadMods(payload);
            Directory.CreateDirectory(projectDataRoot.ModsDirectoryPath);
            Directory.CreateDirectory(backup);
            var moves = new Stack<(string Source, string Destination)>();
            void Move(string sourcePath, string destinationPath)
            {
                Directory.Move(sourcePath, destinationPath);
                moves.Push((destinationPath, sourcePath));
            }

            try
            {
                foreach (var mod in mods)
                {
                    var destination = Path.Combine(projectDataRoot.ModsDirectoryPath, mod.ModId);
                    if (Directory.Exists(destination))
                    {
                        if ((File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0)
                            throw new IOException($"MOD 目录不能是符号链接：{mod.ModId}");
                        Move(destination, Path.Combine(backup, mod.ModId));
                    }

                    Move(mod.ModDirectoryPath, destination);
                }
            }
            catch
            {
                // Preserve backups if rollback itself fails, so installed content remains recoverable.
                cleanWorkspace = false;
                foreach (var (sourcePath, destinationPath) in moves)
                    Directory.Move(sourcePath, destinationPath);
                cleanWorkspace = true;
                throw;
            }

            return mods.Select(mod => mod.ModId).ToArray();
        }
        finally
        {
            if (cleanWorkspace)
                Directory.Delete(workspace, recursive: true);
        }
    }

    private ModContext[] LoadMods(string payload)
    {
        var directories = File.Exists(Path.Combine(payload, ModManifest.FileName))
            ? [payload]
            : Directory.GetDirectories(payload).Order(StringComparer.Ordinal).ToArray();
        if (directories.Length == 0)
            throw new InvalidDataException("压缩包中没有 MOD，请提供包含 mod.json 的 MOD 文件夹。");

        var mods = directories.Select(path => ModRegistry.LoadMod(projectDataRoot, path)).ToArray();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            if (mod.ModId is "." or ".." || !ids.Add(mod.ModId))
                throw new InvalidDataException($"压缩包中的 MOD ID 无效或重复：{mod.ModId}");
        }

        return mods;
    }

    private static void Extract(ZipArchive archive, string destination)
    {
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            var parts = name.TrimEnd('/').Split('/');
            if (parts.Any(part => part is "" or "." or ".." ||
                                  part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.Contains(':')) ||
                ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                throw new InvalidDataException($"压缩包包含不安全的路径：{entry.FullName}");

            var target = Path.GetFullPath(Path.Combine(destination, Path.Combine(parts)));
            if (!target.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException($"压缩包路径超出 MOD 目录：{entry.FullName}");

            if (name.EndsWith('/'))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var input = entry.Open();
            using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write);
            input.CopyTo(output);
        }
    }
}
