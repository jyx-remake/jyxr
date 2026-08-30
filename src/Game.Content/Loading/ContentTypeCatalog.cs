using System.Text.Json;

namespace Game.Content.Loading;

internal enum ContentTypeSourceKind
{
    File,
    Directory,
}

internal sealed record ContentTypeSpec(
    string Kind,
    string PackagePropertyName,
    ContentTypeSourceKind SourceKind,
    string SourcePath,
    string SearchPattern = "*.json")
{
    public static ContentTypeSpec FromFile(string kind, string packagePropertyName, string fileName) =>
        new(kind, packagePropertyName, ContentTypeSourceKind.File, fileName);

    public static ContentTypeSpec FromDirectory(
        string kind,
        string packagePropertyName,
        string directoryName,
        string searchPattern = "*.json") =>
        new(kind, packagePropertyName, ContentTypeSourceKind.Directory, directoryName, searchPattern);
}

internal static class ContentTypeCatalog
{
    public static IReadOnlyList<ContentTypeSpec> All { get; } = Create();

    private static IReadOnlyList<ContentTypeSpec> Create()
    {
        ContentTypeSpec[] specs =
        [
            ContentTypeSpec.FromFile("battle", "battles", "battles.json"),
            ContentTypeSpec.FromFile("scopedBattleEffect", "scopedBattleEffects", "scoped-battle-effects.json"),
            ContentTypeSpec.FromFile("character", "characters", "characters.json"),
            ContentTypeSpec.FromFile("externalSkill", "externalSkills", "external-skills.json"),
            ContentTypeSpec.FromFile("gameTip", "gameTips", "game-tips.json"),
            ContentTypeSpec.FromFile("growTemplate", "growTemplates", "grow-templates.json"),
            ContentTypeSpec.FromFile("internalSkill", "internalSkills", "internal-skills.json"),
            ContentTypeSpec.FromFile("legendSkill", "legendSkills", "legend-skills.json"),
            ContentTypeSpec.FromDirectory("map", "maps", "maps"),
            ContentTypeSpec.FromFile("worldTrigger", "worldTriggers", "world-triggers.json"),
            ContentTypeSpec.FromFile("resource", "resources", "resources.json"),
            ContentTypeSpec.FromFile("sect", "sects", "sects.json"),
            ContentTypeSpec.FromFile("shop", "shops", "shops.json"),
            ContentTypeSpec.FromFile("specialSkill", "specialSkills", "special-skills.json"),
            ContentTypeSpec.FromFile("item", "items", "items.json"),
            ContentTypeSpec.FromFile("itemTag", "itemTags", "item-tags.json"),
            ContentTypeSpec.FromFile("equipmentRandomAffixTable", "randomAffixTables", "random-affix-tables.json"),
            ContentTypeSpec.FromFile("buff", "buffs", "buffs.json"),
            ContentTypeSpec.FromFile("talent", "talents", "talents.json"),
            ContentTypeSpec.FromFile("characterTitle", "characterTitles", "character-titles.json"),
            ContentTypeSpec.FromFile("tower", "towers", "towers.json"),
        ];

        Validate(specs);
        return specs;
    }

    private static void Validate(IReadOnlyList<ContentTypeSpec> specs)
    {
        EnsureUnique(specs.Select(static spec => spec.Kind), "kind");
        EnsureUnique(specs.Select(static spec => spec.PackagePropertyName), "package property");
        EnsureUnique(specs.Select(static spec => $"{spec.SourceKind}:{spec.SourcePath}"), "source");

        var registeredProperties = specs
            .Select(static spec => spec.PackagePropertyName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var definitionProperties = typeof(ContentPackage)
            .GetProperties()
            .Where(static property =>
                property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                property.PropertyType.GetGenericArguments()[0].Name.EndsWith("Definition", StringComparison.Ordinal))
            .Select(static property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        if (!registeredProperties.SequenceEqual(definitionProperties, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "Content type catalog must explicitly register every definition collection in ContentPackage.");
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string description)
    {
        var duplicate = values.GroupBy(static value => value, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Content type catalog contains duplicate {description} '{duplicate.Key}'.");
        }
    }
}
