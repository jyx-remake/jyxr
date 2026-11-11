using Game.Core.Story;

namespace Game.StoryDsl;

/// <summary>Compiles Story source directly to the runtime story model.</summary>
public static class StoryCompiler
{
    public static StoryScript Compile(string source, string sourceName = "story")
    {
        ArgumentNullException.ThrowIfNull(source);
        return new StoryParser(source, sourceName).Parse();
    }
}
