namespace Game.Application;

public static class AtomicTextFile
{
    public static void Write(string path, string content)
    {
        var absolutePath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var temporaryPath = absolutePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, leaveOpen: true))
                {
                    writer.Write(content);
                    writer.Flush();
                }
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, absolutePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
