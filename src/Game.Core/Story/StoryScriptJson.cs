using System.Text.Json;

namespace Game.Core.Story;

public static class StoryScriptJson
{
    public static StoryScript LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static StoryScript Load(Stream stream)
    {
        using var document = JsonDocument.Parse(stream);
        return new StoryScriptJsonParser(document.RootElement).Parse();
    }

    public static StoryScript Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new StoryScriptJsonParser(document.RootElement).Parse();
    }
}
