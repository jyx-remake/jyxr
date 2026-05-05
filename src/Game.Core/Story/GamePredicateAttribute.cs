namespace Game.Core.Story;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class GamePredicateAttribute : Attribute
{
    public GamePredicateAttribute(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length == 0)
        {
            throw new ArgumentException("Game predicate must declare at least one name.", nameof(names));
        }

        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (!uniqueNames.Add(name))
            {
                throw new ArgumentException($"Duplicate predicate alias '{name}'.", nameof(names));
            }
        }

        Names = names;
    }

    public IReadOnlyList<string> Names { get; }
}
