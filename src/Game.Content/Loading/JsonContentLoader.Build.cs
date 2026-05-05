using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Affix;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    private const int UnlimitedWuxueStatValue = 9999;

    private static InMemoryContentRepository BuildRepository(ContentPackage package)
    {
        NormalizeCharacterStats(package.Characters);

        var repository = new InMemoryContentRepository
        {
            Battles = IndexById(package.Battles, "Battle"),
            Characters = IndexById(package.Characters, "Character"),
            ExternalSkills = IndexById(package.ExternalSkills, "ExternalSkill"),
            GameTips = IndexById(package.GameTips, "GameTip"),
            GrowTemplates = IndexById(package.GrowTemplates, "GrowTemplate"),
            InternalSkills = IndexById(package.InternalSkills, "InternalSkill"),
            LegendSkills = package.LegendSkills.ToList(),
            Maps = IndexById(package.Maps, "Map"),
            Resources = IndexById(package.Resources, "Resource"),
            Sects = IndexOrderedById(package.Sects, "Sect"),
            Shops = IndexById(package.Shops, "Shop"),
            SpecialSkills = IndexById(package.SpecialSkills, "SpecialSkill"),
            StoryScripts = new Dictionary<string, StoryScript>(package.StoryScripts, StringComparer.Ordinal),
            StorySegments = BuildStorySegments(package.StoryScripts),
            Items = BuildItems(package),
            Buffs = IndexById(package.Buffs, "Buff"),
            Talents = IndexById(package.Talents, "Talent"),
            Equipments = IndexById(package.Items.OfType<EquipmentDefinition>(), "Equipment"),
            Towers = IndexById(package.Towers, "Tower"),
        };

        ResolveAffixes(repository);

        foreach (var definition in repository.SpecialSkills.Values)
        {
            definition.Resolve(repository);
        }

        foreach (var definition in repository.ExternalSkills.Values)
        {
            definition.Resolve(repository);
        }

        foreach (var definition in repository.InternalSkills.Values)
        {
            definition.Resolve(repository);
        }

        foreach (var definition in repository.Characters.Values)
        {
            definition.Resolve(repository);
        }

        foreach (var definition in repository.LegendSkills)
        {
            definition.Resolve(repository);
        }

        return repository;
    }

    private static void ResolveAffixes(InMemoryContentRepository repository)
    {
        var providers = repository.Talents.Values
            .Cast<IAffixProvider>()
            .Concat(repository.Buffs.Values)
            .Concat(repository.Equipments.Values);

        foreach (var provider in providers)
        {
            foreach (var affix in provider.Affixes)
            {
                affix.Resolve(repository);
            }
        }

        foreach (var skill in repository.ExternalSkills.Values)
        {
            foreach (var affix in skill.Affixes)
            {
                var effect = affix.Effect;
                Ensure(effect is not null, $"ExternalSkill '{skill.Id}' has skill affix without effect.");
                effect!.Resolve(repository);
            }
        }

        foreach (var skill in repository.InternalSkills.Values)
        {
            foreach (var affix in skill.Affixes)
            {
                var effect = affix.Effect;
                Ensure(effect is not null, $"InternalSkill '{skill.Id}' has skill affix without effect.");
                effect!.Resolve(repository);
            }
        }
    }

    private static Dictionary<string, ItemDefinition> BuildItems(ContentPackage package) =>
        package.Items.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal);

    private static void NormalizeCharacterStats(List<CharacterDefinition> characters)
    {
        for (var index = 0; index < characters.Count; index++)
        {
            var character = characters[index];
            if (!character.Stats.TryGetValue(StatType.Wuxue, out var wuxue) || wuxue != -1)
            {
                continue;
            }

            var stats = character.Stats.ToDictionary(
                entry => entry.Key,
                entry => entry.Value,
                EqualityComparer<StatType>.Default);
            stats[StatType.Wuxue] = UnlimitedWuxueStatValue;
            characters[index] = character with { Stats = stats };
        }
    }

    private static Dictionary<string, StorySegmentEntry> BuildStorySegments(
        IReadOnlyDictionary<string, StoryScript> storyScripts)
    {
        var segments = new Dictionary<string, StorySegmentEntry>(StringComparer.Ordinal);
        var allSegments = storyScripts
            .SelectMany(static entry => entry.Value.Segments)
            .ToList();
        var combinedScript = new StoryScript(
            storyScripts.Values.FirstOrDefault()?.Version ?? 1,
            allSegments);

        foreach (var (scriptId, script) in storyScripts)
        {
            foreach (var segment in script.Segments)
            {
                Ensure(segments.TryAdd(segment.Name, new StorySegmentEntry(segment.Name, scriptId, combinedScript, segment)),
                    $"Story segment '{segment.Name}' is duplicated.");
            }
        }

        return segments;
    }

    private static Dictionary<string, TDefinition> IndexById<TDefinition>(IEnumerable<TDefinition> definitions, string typeName)
        where TDefinition : class
    {
        var indexed = new Dictionary<string, TDefinition>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            var id = (string?)typeof(TDefinition).GetProperty("Id")?.GetValue(definition);
            Ensure(!string.IsNullOrWhiteSpace(id), $"{typeName} definition has empty id.");
            Ensure(indexed.TryAdd(id!, definition), $"{typeName} '{id}' is duplicated.");
        }

        return indexed;
    }

    private static OrderedDictionary<string, TDefinition> IndexOrderedById<TDefinition>(
        IEnumerable<TDefinition> definitions,
        string typeName)
        where TDefinition : class =>
        new(
            definitions.Select(definition => KeyValuePair.Create(GetDefinitionId(definition, typeName), definition)),
            StringComparer.Ordinal);

    private static string GetDefinitionId<TDefinition>(TDefinition definition, string typeName)
        where TDefinition : class
    {
        var id = (string?)typeof(TDefinition).GetProperty("Id")?.GetValue(definition);
        Ensure(!string.IsNullOrWhiteSpace(id), $"{typeName} definition has empty id.");
        return id!;
    }
}
