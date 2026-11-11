using System.Text;
using Game.StoryDsl;

namespace Game.Tools.StoryXml;

public static class StoryXmlConverterCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            WriteUsage(output);
            return 0;
        }
        if (args.Length is < 1 or > 2)
        {
            WriteUsage(error);
            return 1;
        }

        try
        {
            var inputPath = Path.GetFullPath(args[0]);
            var source = LegacyStoryXmlConverter.Convert(File.ReadAllText(inputPath), inputPath);
            var outputPath = WriteOutput(inputPath, args.Length == 2 ? Path.GetFullPath(args[1]) : null, source);
            output.WriteLine($"{inputPath} -> {outputPath}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                         FormatException or StoryCompilationException or ArgumentException)
        {
            error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string WriteOutput(string inputPath, string? explicitOutputPath, string source)
    {
        var outputPath = explicitOutputPath ?? Path.Combine(
            Path.GetDirectoryName(inputPath)!, Path.GetFileNameWithoutExtension(inputPath) + ".converted.story");
        var temporaryPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $".story-conversion-{Guid.NewGuid():N}.tmp");
        var temporaryStream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            using (temporaryStream)
            {
                temporaryStream.Write(new UTF8Encoding(false, true).GetBytes(source));
            }

            for (var suffix = 2; ; suffix++)
            {
                try
                {
                    File.Move(temporaryPath, outputPath);
                    return outputPath;
                }
                catch (IOException) when (explicitOutputPath is null &&
                                          (File.Exists(outputPath) || Directory.Exists(outputPath)))
                {
                    outputPath = Path.Combine(Path.GetDirectoryName(inputPath)!,
                        $"{Path.GetFileNameWithoutExtension(inputPath)}.converted-{suffix}.story");
                }
            }
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("用法 dotnet run --project tools/StoryXmlConverter -- <input.xml> [output.story]");
        writer.WriteLine("省略输出路径时自动选用 <input>.converted.story 或递增编号的可用文件名。");
        writer.WriteLine("指定的输出路径须可供新建文件，转换结果通过剧情编译器校验后写入。");
    }
}
