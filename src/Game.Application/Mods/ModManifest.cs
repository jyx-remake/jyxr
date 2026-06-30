using System.Globalization;
using System.Text.Json.Serialization;

namespace Game.Application.Mods;

public sealed record ModManifest(
    string Id,
    string Name,
    string Version,
    string? Date = null,
    string? Description = null,
    string? Author = null,
    IReadOnlyList<string>? Packs = null,
    IReadOnlyList<string>? Assemblies = null,
    string? MinClientVersion = null)
{
    public const string FileName = "mod.json";
    public const string DataDirectoryName = "data";

    [JsonIgnore]
    public IReadOnlyList<string> ResolvedPacks => NormalizeRelativePaths(Packs);

    [JsonIgnore]
    public IReadOnlyList<string> ResolvedAssemblies => NormalizeRelativePaths(Assemblies);

    public void Validate()
    {
        EnsureStableId(Id, nameof(Id));
        EnsureRequired(Name, nameof(Name));
        EnsureRequired(Version, nameof(Version));
        EnsureDate(Date, nameof(Date));
        _ = ResolvedPacks;
        _ = ResolvedAssemblies;
    }

    private static IReadOnlyList<string> NormalizeRelativePaths(IReadOnlyList<string>? paths) =>
        paths is null
            ? []
            : paths.Select(path => NormalizeRelativePath(path, ""))
                .Where(path => path.Length > 0)
                .ToArray();

    private static string NormalizeRelativePath(string? path, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(path)
            ? fallback
            : path.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(normalized) ||
            normalized.StartsWith("res://", StringComparison.Ordinal) ||
            normalized.StartsWith("user://", StringComparison.Ordinal) ||
            normalized.Split('/').Any(static part => part == ".."))
        {
            throw new InvalidOperationException($"Mod manifest path must be relative and stay inside the mod directory: {path}");
        }

        return normalized;
    }

    private static void EnsureRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Mod manifest field '{fieldName}' is required.");
        }
    }

    private static void EnsureDate(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new InvalidOperationException($"Mod manifest field '{fieldName}' must use yyyy-MM-dd format.");
        }
    }

    private static void EnsureStableId(string? value, string fieldName)
    {
        EnsureRequired(value, fieldName);
        if (value!.Any(static c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
        {
            throw new InvalidOperationException(
                $"Mod manifest field '{fieldName}' must contain only ASCII letters, digits, '-', '_' or '.'.");
        }
    }
}
