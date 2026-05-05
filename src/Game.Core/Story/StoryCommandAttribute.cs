namespace Game.Core.Story;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class StoryCommandAttribute : Attribute
{
    public StoryCommandAttribute(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length == 0)
        {
            throw new ArgumentException("Story command must declare at least one name.", nameof(names));
        }

        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!uniqueNames.Add(name))
            {
                throw new ArgumentException($"Duplicate story command alias '{name}'.", nameof(names));
            }
        }

        Names = names;
    }

    public IReadOnlyList<string> Names { get; }
}
