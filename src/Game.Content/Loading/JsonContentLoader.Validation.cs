using System.Text.Json;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Story;
using Game.Expressions;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    private const string AchievementResourceGroup = "nick";
    private const string AchievementResourcePrefix = AchievementResourceGroup + ".";
    private static readonly ExpressionFunctionRegistry RandomAffixExpressionFunctions =
        new ExpressionFunctionRegistryBuilder()
            .AddLibrary(new CoreExpressionFunctions())
            .Build();
    private static readonly IReadOnlyDictionary<string, ExpressionValueKind> RandomAffixVariables =
        new Dictionary<string, ExpressionValueKind>(StringComparer.Ordinal)
        {
            ["item_level"] = ExpressionValueKind.Number,
            ["round"] = ExpressionValueKind.Number,
        };
    private static readonly IReadOnlyDictionary<string, ExpressionValueKind> RandomAffixCandidateVariables =
        new Dictionary<string, ExpressionValueKind>(StringComparer.Ordinal)
        {
            ["item_level"] = ExpressionValueKind.Number,
            ["round"] = ExpressionValueKind.Number,
            ["skill_hard"] = ExpressionValueKind.Number,
        };

    private static void ValidateRepository(InMemoryContentRepository repository)
    {
        ValidateResources(repository);
        ValidateMediaReferences(repository);
        ValidateCharacters(repository);
        ValidateCharacterTitles(repository);
        ValidateBattles(repository);
        ValidateBattleHookAffixes(repository);
        ValidateScopedBattleEffects(repository);
        ValidateSkillBuffs(repository);
        ValidateSkillAffixes(repository);
        ValidateSpecialSkills(repository);
        ValidateItemReferences(repository);
        ValidateEquipmentRandomAffixTables(repository);
        ValidateShops(repository);
        ValidateLegendSkills(repository);
        ValidateWorldTriggers(repository);
        ValidateMaps(repository);
        ValidateTowers(repository);
        ValidateStoryContent(repository);
    }

    private static void ValidateCharacterTitles(InMemoryContentRepository repository)
    {
        foreach (var title in repository.CharacterTitles.Values)
        {
            Ensure(!string.IsNullOrWhiteSpace(title.Id), "CharacterTitle definition has empty id.");
            Ensure(!string.IsNullOrWhiteSpace(title.Name), $"CharacterTitle '{title.Id}' has empty name.");
            Ensure(title.Affixes is not null, $"CharacterTitle '{title.Id}' has null affixes.");
            if (title.Affixes is null)
            {
                continue;
            }

            foreach (var affix in title.Affixes)
            {
                Ensure(affix is not null, $"CharacterTitle '{title.Id}' contains a null affix.");
            }
        }
    }

    private static void ValidateMaps(InMemoryContentRepository repository)
    {
        foreach (var map in repository.Maps.Values)
        {
            var locationsById = new Dictionary<string, MapLocationDefinition>(StringComparer.Ordinal);
            foreach (var location in map.Locations)
            {
                Ensure(locationsById.TryAdd(location.Id, location),
                    $"Map '{map.Id}' contains duplicate location id '{location.Id}'.");
            }

            if (map.Kind == MapKind.Large)
            {
                Ensure(double.IsFinite(map.TravelSpeed) && map.TravelSpeed > 0d,
                    $"Large map '{map.Id}' must have a positive finite travelSpeed.");
                // A few legacy maps are background-only variants (for example
                // the night view of the Forbidden City) and intentionally have
                // no map units. They still use the large-map renderer, but do
                // not have a meaningful landing location.
                if (map.Locations.Count > 0)
                {
                    Ensure(!string.IsNullOrWhiteSpace(map.DefaultLocation),
                        $"Large map '{map.Id}' must define defaultLocation.");
                    Ensure(locationsById.ContainsKey(map.DefaultLocation!),
                        $"Large map '{map.Id}' defaultLocation '{map.DefaultLocation}' does not reference one of its locations.");
                }
            }
            else
            {
                Ensure(map.TravelSpeed == 0d,
                    $"Small map '{map.Id}' cannot define travelSpeed.");
                Ensure(map.DefaultLocation is null,
                    $"Small map '{map.Id}' cannot define defaultLocation.");
            }

            foreach (var location in map.Locations)
            {
                var owner = $"Map '{map.Id}' location '{location.Id}'";
                var eventIds = new HashSet<string>(StringComparer.Ordinal);
                if (map.Kind == MapKind.Large)
                {
                    Ensure(location.Position is not null, $"{owner} must define a position.");
                    var position = location.Position!.Value;
                    Ensure(
                        position.X >= 0 && position.X <= LargeMapCoordinateSpace.Width &&
                        position.Y >= 0 && position.Y <= LargeMapCoordinateSpace.Height,
                        $"{owner} position ({position.X}, {position.Y}) exceeds large-map coordinate space " +
                        $"{LargeMapCoordinateSpace.Width}x{LargeMapCoordinateSpace.Height}.");
                }

                if (location.NoEventImage is not null)
                {
                    Ensure(!string.IsNullOrWhiteSpace(location.NoEventImage),
                        $"{owner} has an empty noEventImage.");
                }

                Ensure(!location.HideWhenNoEvent || location.NoEventImage is null,
                    $"{owner} cannot define noEventImage when hideWhenNoEvent is true.");

                foreach (var mapEvent in location.Events)
                {
                    Ensure(!string.IsNullOrWhiteSpace(mapEvent.Id), $"{owner} has an event with an empty id.");
                    Ensure(eventIds.Add(mapEvent.Id),
                        $"{owner} contains duplicate event id '{mapEvent.Id}'.");
                    Ensure(mapEvent.RepeatLimit is null || mapEvent.RepeatMode == RepeatMode.Once,
                        $"{owner} event '{mapEvent.Id}' defines repeatLimit without repeatMode 'once'.");
                    Ensure(mapEvent.RepeatLimit is null || mapEvent.RepeatLimit == -1 || mapEvent.RepeatLimit > 0,
                        $"{owner} event '{mapEvent.Id}' repeatLimit must be -1 or a positive integer.");
                }
            }
        }
    }

    private static void ValidateResources(InMemoryContentRepository repository)
    {
        foreach (var resource in repository.Resources.Values)
        {
            Ensure(!resource.Id.Contains('/'),
                $"Resource ID '{resource.Id}' cannot contain '/'.");
            Ensure(resource.Tags is not null, $"Resource '{resource.Id}' has null tags.");
            var tags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tag in resource.Tags!)
            {
                Ensure(!string.IsNullOrWhiteSpace(tag), $"Resource '{resource.Id}' has an empty tag.");
                Ensure(tags.Add(tag), $"Resource '{resource.Id}' contains tag '{tag}' more than once.");
            }
        }
    }

    private static void ValidateMediaReferences(InMemoryContentRepository repository)
    {
        foreach (var character in repository.Characters.Values)
        {
            ValidateOptionalMediaReference(character.Portrait, MediaAssetKind.Texture,
                $"Character '{character.Id}' portrait", repository);
        }

        foreach (var item in repository.Items.Values)
        {
            ValidateOptionalMediaReference(item.Picture, MediaAssetKind.Texture,
                $"Item '{item.Id}' picture", repository);
        }

        foreach (var skill in repository.ExternalSkills.Values)
        {
            ValidateSkillMediaReferences(skill.Id, skill.Icon, skill.Audio, skill.FormSkills, repository);
        }

        foreach (var skill in repository.InternalSkills.Values)
        {
            ValidateSkillMediaReferences(skill.Id, skill.Icon, null, skill.FormSkills, repository);
        }

        foreach (var skill in repository.SpecialSkills.Values)
        {
            ValidateOptionalMediaReference(skill.Icon, MediaAssetKind.Texture,
                $"SpecialSkill '{skill.Id}' icon", repository);
            ValidateOptionalMediaReference(skill.Audio, MediaAssetKind.Audio,
                $"SpecialSkill '{skill.Id}' audio", repository);
        }

        foreach (var battle in repository.Battles.Values)
        {
            ValidateBattleCharacterLists(battle, repository);
            ValidateRequiredMediaReference(battle.Background, MediaAssetKind.Texture,
                $"Battle '{battle.Id}' background", repository);
            ValidateOptionalMediaReference(battle.Music, MediaAssetKind.Audio,
                $"Battle '{battle.Id}' music", repository);
        }

        foreach (var map in repository.Maps.Values)
        {
            ValidateOptionalMediaReference(map.Picture, MediaAssetKind.Texture,
                $"Map '{map.Id}' picture", repository);
            foreach (var music in map.Musics)
            {
                ValidateRequiredMediaReference(music, MediaAssetKind.Audio,
                    $"Map '{map.Id}' music", repository);
            }

            foreach (var location in map.Locations)
            {
                var owner = $"Map '{map.Id}' location '{location.Id}'";
                ValidateOptionalMediaReference(location.Picture, MediaAssetKind.Texture,
                    $"{owner} picture", repository);
                ValidateOptionalMediaReference(location.NoEventImage, MediaAssetKind.Texture,
                    $"{owner} noEventImage", repository);
                foreach (var mapEvent in location.Events)
                {
                    ValidateOptionalMediaReference(mapEvent.Image, MediaAssetKind.Texture,
                        $"{owner} event '{mapEvent.Id}' image", repository);
                }
            }
        }

        foreach (var shop in repository.Shops.Values)
        {
            ValidateOptionalMediaReference(shop.Background, MediaAssetKind.Texture,
                $"Shop '{shop.Id}' background", repository);
            ValidateOptionalMediaReference(shop.Music, MediaAssetKind.Audio,
                $"Shop '{shop.Id}' music", repository);
        }

        foreach (var sect in repository.Sects.Values)
        {
            ValidateOptionalMediaReference(sect.Portrait, MediaAssetKind.Texture,
                $"Sect '{sect.Id}' portrait", repository);
            ValidateOptionalMediaReference(sect.Background, MediaAssetKind.Texture,
                $"Sect '{sect.Id}' background", repository);
        }
    }

    private static void ValidateBattleCharacterLists(BattleDefinition battle, InMemoryContentRepository repository)
    {
        var required = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in battle.RequiredCharacterIds)
        {
            Ensure(!string.IsNullOrWhiteSpace(id), $"Battle '{battle.Id}' has an empty required character id.");
            Ensure(required.Add(id), $"Battle '{battle.Id}' repeats required character '{id}'.");
            Ensure(repository.Characters.ContainsKey(id) || repository.Characters.Values.Any(c => c.Name == id),
                $"Battle '{battle.Id}' references missing required character '{id}'.");
        }
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in battle.ExcludedCharacterIds)
        {
            Ensure(!string.IsNullOrWhiteSpace(id), $"Battle '{battle.Id}' has an empty excluded character id.");
            Ensure(excluded.Add(id), $"Battle '{battle.Id}' repeats excluded character '{id}'.");
            Ensure(!required.Contains(id), $"Battle '{battle.Id}' cannot both require and exclude character '{id}'.");
        }
    }

    private static void ValidateSkillMediaReferences(
        string skillId,
        string? icon,
        string? audio,
        IReadOnlyList<FormSkillDefinition> forms,
        InMemoryContentRepository repository)
    {
        ValidateOptionalMediaReference(icon, MediaAssetKind.Texture,
            $"Skill '{skillId}' icon", repository);
        ValidateOptionalMediaReference(audio, MediaAssetKind.Audio,
            $"Skill '{skillId}' audio", repository);
        foreach (var form in forms)
        {
            ValidateOptionalMediaReference(form.Icon, MediaAssetKind.Texture,
                $"Skill '{skillId}' form '{form.Id}' icon", repository);
            ValidateOptionalMediaReference(form.Audio, MediaAssetKind.Audio,
                $"Skill '{skillId}' form '{form.Id}' audio", repository);
        }
    }

    internal static void ValidateGameConfigMediaReferences(
        Game.Core.Model.GameConfig config,
        InMemoryContentRepository repository)
    {
        ValidateRequiredMediaReference(config.MainMenuBackground, MediaAssetKind.Texture,
            "Game config mainMenuBackground", repository);
        ValidateRequiredMediaReference(config.MainMenuMusic, MediaAssetKind.Audio,
            "Game config mainMenuMusic", repository);
        foreach (var portrait in config.SelectablePortraitIds)
        {
            ValidateRequiredMediaReference(portrait, MediaAssetKind.Texture,
                "Game config selectablePortraitIds", repository);
        }

        foreach (var music in config.RandomBattleMusics)
        {
            ValidateRequiredMediaReference(music, MediaAssetKind.Audio,
                "Game config randomBattleMusics", repository);
        }
    }

    private static void ValidateOptionalMediaReference(
        string? reference,
        MediaAssetKind assetKind,
        string owner,
        InMemoryContentRepository repository)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        ValidateRequiredMediaReference(reference, assetKind, owner, repository);
    }

    private static void ValidateRequiredMediaReference(
        string? reference,
        MediaAssetKind assetKind,
        string owner,
        InMemoryContentRepository repository)
    {
        var resolution = MediaReferenceResolver.Resolve(reference, assetKind, repository);
        Ensure(resolution.IsSuccess, $"{owner} is invalid: {resolution.Error}");
    }

    private static void ValidateItemTags(InMemoryContentRepository repository)
    {
        foreach (var tag in repository.ItemTags.Values)
        {
            Ensure(!string.IsNullOrWhiteSpace(tag.Id), "ItemTag definition has empty id.");
            Ensure(!string.IsNullOrWhiteSpace(tag.Name), $"ItemTag '{tag.Id}' has empty name.");
            Ensure(tag.Order >= 0, $"ItemTag '{tag.Id}' has invalid order '{tag.Order}'.");
        }

        foreach (var item in repository.Items.Values)
        {
            var itemTagIds = item.TagIds;
            Ensure(itemTagIds is not null, $"Item '{item.Id}' has null item tag ids.");

            var tagIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tagId in itemTagIds!)
            {
                Ensure(!string.IsNullOrWhiteSpace(tagId), $"Item '{item.Id}' has an empty item tag id.");
                Ensure(tagIds.Add(tagId), $"Item '{item.Id}' references item tag '{tagId}' more than once.");
                Ensure(repository.ItemTags.ContainsKey(tagId),
                    $"Item '{item.Id}' references missing item tag '{tagId}'.");
            }
        }
    }

    private static void ValidateSkillBuffs(InMemoryContentRepository repository)
    {
        foreach (var skill in repository.ExternalSkills.Values)
        {
            ValidateSkillBuffs(skill.Buffs, $"ExternalSkill '{skill.Id}'");
            foreach (var formSkill in skill.FormSkills)
            {
                ValidateSkillBuffs(formSkill.Buffs, $"FormSkill '{formSkill.Id}'");
            }
        }

        foreach (var skill in repository.SpecialSkills.Values)
        {
            ValidateSkillBuffs(skill.Buffs, $"SpecialSkill '{skill.Id}'");
        }

        foreach (var skill in repository.LegendSkills)
        {
            ValidateSkillBuffs(skill.Buffs, $"LegendSkill '{skill.Id}'");
        }
    }

    private static void ValidateSkillBuffs(
        IEnumerable<SkillBuffDefinition> buffs,
        string ownerName)
    {
        foreach (var buff in buffs)
        {
            Ensure(buff.Level >= 0, $"{ownerName} has buff '{buff.Id}' with invalid level '{buff.Level}'.");
            Ensure(buff.Duration >= 1, $"{ownerName} has buff '{buff.Id}' with invalid duration '{buff.Duration}'.");
            if (buff.Chance is { } chance)
            {
                Ensure(chance is >= 0 and <= 100,
                    $"{ownerName} has buff '{buff.Id}' with invalid chance '{chance}'.");
            }
        }
    }

    private static void ValidateWorldTriggers(InMemoryContentRepository repository)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var trigger in repository.WorldTriggers)
        {
            Ensure(!string.IsNullOrWhiteSpace(trigger.Id), "WorldTrigger definition has empty id.");
            Ensure(ids.Add(trigger.Id), $"WorldTrigger '{trigger.Id}' is duplicated.");
        }
    }

    private static void ValidateCharacters(InMemoryContentRepository repository)
    {
        foreach (var character in repository.Characters.Values)
        {
            Ensure(character.InternalSkills.Count(skill => skill.Equipped) <= 1,
                $"Character '{character.Id}' has more than one equipped internal skill.");
        }
    }

    private static void ValidateEquipmentRandomAffixTables(InMemoryContentRepository repository)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in repository.EquipmentRandomAffixTables)
        {
            Ensure(!string.IsNullOrWhiteSpace(table.Id),
                "Equipment random affix table has empty id.");
            Ensure(ids.Add(table.Id),
                $"Equipment random affix table '{table.Id}' is duplicated.");
            ValidateRandomAffixExpression(
                table.When,
                RandomAffixVariables,
                ExpressionValueKind.Boolean,
                $"Equipment random affix table '{table.Id}' when");
            Ensure(table.Options.Count > 0,
                $"Equipment random affix table '{table.Id}' has no options.");

            foreach (var option in table.Options)
            {
                Ensure(option.Weight > 0,
                    $"Equipment random affix option '{option.Kind}' must have positive weight.");
                var usesPool = option.Kind is EquipmentRandomAffixKind.Talent or EquipmentRandomAffixKind.Speed;
                Ensure(usesPool == (option.Pool.Count > 0),
                    usesPool
                        ? $"Equipment random affix option '{option.Kind}' must declare a non-empty pool."
                        : $"Equipment random affix option '{option.Kind}' cannot declare a pool.");
                Ensure((option.Kind == EquipmentRandomAffixKind.WeaponBonus) == (option.WeaponType is not null),
                    option.Kind == EquipmentRandomAffixKind.WeaponBonus
                        ? "Equipment random affix weapon bonus option must declare weaponType."
                        : $"Equipment random affix option '{option.Kind}' cannot declare weaponType.");

                var usesHard = IsSkillRandomAffix(option.Kind);
                Ensure(usesHard == (option.CandidateWhen is not null),
                    usesHard
                        ? $"Equipment random affix option '{option.Kind}' must declare candidateWhen."
                        : $"Equipment random affix option '{option.Kind}' cannot declare candidateWhen.");
                if (option.CandidateWhen is not null)
                {
                    ValidateRandomAffixExpression(
                        option.CandidateWhen,
                        RandomAffixCandidateVariables,
                        ExpressionValueKind.Boolean,
                        $"Equipment random affix option '{option.Kind}' candidateWhen");
                }

                var expectedRangeCount = GetRandomAffixRangeCount(option.Kind);
                Ensure(option.Ranges.Count == expectedRangeCount,
                    $"Equipment random affix option '{option.Kind}' requires {expectedRangeCount} ranges, got {option.Ranges.Count}.");
                var rangeVariables = usesHard ? RandomAffixCandidateVariables : RandomAffixVariables;
                for (var rangeIndex = 0; rangeIndex < option.Ranges.Count; rangeIndex++)
                {
                    var range = option.Ranges[rangeIndex];
                    ValidateRandomAffixExpression(
                        range.Min,
                        rangeVariables,
                        ExpressionValueKind.Number,
                        $"Equipment random affix option '{option.Kind}' range {rangeIndex} min");
                    ValidateRandomAffixExpression(
                        range.Max,
                        rangeVariables,
                        ExpressionValueKind.Number,
                        $"Equipment random affix option '{option.Kind}' range {rangeIndex} max");
                    if (range.Mode == EquipmentRandomAffixRangeMode.Integer)
                    {
                        Ensure(range.DecimalPlaces == 0,
                            $"Equipment random affix option '{option.Kind}' integer range cannot declare decimalPlaces.");
                    }
                    else
                    {
                        Ensure(range.DecimalPlaces is >= 1 and <= 15,
                            $"Equipment random affix option '{option.Kind}' decimal range requires decimalPlaces from 1 to 15.");
                    }
                }

                if (option.Kind == EquipmentRandomAffixKind.Talent)
                {
                    Ensure(option.Pool.Count > 0,
                        "Equipment random affix talent option must have a non-empty pool.");
                    foreach (var talentId in option.Pool)
                    {
                        Ensure(repository.Talents.ContainsKey(talentId),
                            $"Equipment random affix talent '{talentId}' does not exist.");
                    }
                }

                if (option.Kind == EquipmentRandomAffixKind.Speed)
                {
                    Ensure(option.Pool.Count > 0,
                        "Equipment random affix speed option must have a non-empty pool.");
                    foreach (var value in option.Pool)
                    {
                        Ensure(double.TryParse(value, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed),
                            $"Equipment random affix speed value '{value}' is invalid.");
                    }
                }
            }
        }
    }

    private static bool IsSkillRandomAffix(EquipmentRandomAffixKind kind) => kind is
        EquipmentRandomAffixKind.ExternalSkillBonus or
        EquipmentRandomAffixKind.InternalSkillBonus or
        EquipmentRandomAffixKind.FormSkillBonus or
        EquipmentRandomAffixKind.LegendSkillBonus;

    private static int GetRandomAffixRangeCount(EquipmentRandomAffixKind kind) => kind switch
    {
        EquipmentRandomAffixKind.AttackCombo or EquipmentRandomAffixKind.DefenceCombo or
            EquipmentRandomAffixKind.LegendSkillBonus => 2,
        EquipmentRandomAffixKind.Talent or EquipmentRandomAffixKind.Speed => 0,
        _ => 1,
    };

    private static void ValidateRandomAffixExpression(
        ParsedExpression expression,
        IReadOnlyDictionary<string, ExpressionValueKind> variables,
        ExpressionValueKind expectedKind,
        string context)
    {
        var diagnostics = new ExpressionAnalyzer().Analyze(
            expression.Root,
            RandomAffixExpressionFunctions,
            variables,
            expectedKind);
        var errors = diagnostics
            .Where(static diagnostic => diagnostic.Severity == ExpressionDiagnosticSeverity.Error)
            .Select(static diagnostic => diagnostic.Message)
            .ToArray();
        Ensure(errors.Length == 0, $"{context} is invalid: {string.Join("; ", errors)}");
    }

    private static void ValidateSpecialSkills(InMemoryContentRepository repository)
    {
        foreach (var skill in repository.SpecialSkills.Values)
        {
            ValidateSpecialSkillSpeech(skill, repository);

            foreach (var effect in skill.Effects ?? [])
            {
                switch (effect)
                {
                    case ModifyDamageBattleHookEffectDefinition:
                    case ModifyDamageContextBattleHookEffectDefinition:
                    case ModifyMpCostBattleHookEffectDefinition:
                    case ModifyRecoveryBattleHookEffectDefinition:
                    case StrengthenContextBuffBattleHookEffectDefinition:
                    case CancelHitBattleHookEffectDefinition:
                    case SetHitSuccessBattleHookEffectDefinition:
                        throw new InvalidOperationException(
                            $"SpecialSkill '{skill.Id}' uses unsupported hook-only effect '{effect.GetType().Name}'.");
                    case CustomAbilityBattleEffectDefinition custom:
                        Ensure(custom.SupportsAbility,
                            $"SpecialSkill '{skill.Id}' uses custom effect '{custom.EffectId}' that does not support ability execution.");
                        ValidateBattleUnitSelector(custom.Target, $"SpecialSkill '{skill.Id}'", null);
                        break;
                    default:
                        ValidateSharedBattleEffect(effect, $"SpecialSkill '{skill.Id}'", repository);
                        break;
                }
            }
        }
    }

    private static void ValidateSpecialSkillSpeech(
        SpecialSkillDefinition skill,
        InMemoryContentRepository repository)
    {
        if (skill.Speech is null)
        {
            return;
        }

        ValidateBattleSpeech(skill.Speech, $"SpecialSkill '{skill.Id}'");
        Ensure(skill.Speech.Speaker == BattleSpeechSpeaker.Owner,
            $"SpecialSkill '{skill.Id}' speech speaker must be owner.");
    }

    private static void ValidateBattles(InMemoryContentRepository repository)
    {
        foreach (var battle in repository.Battles.Values)
        {
            var occupiedPositions = new HashSet<GridPosition>();
            foreach (var participant in battle.Participants)
            {
                ValidateBattlePosition(
                    participant.Position,
                    occupiedPositions,
                    $"Battle '{battle.Id}' participant");
            }

            foreach (var participant in battle.RandomParticipants)
            {
                ValidateBattlePosition(
                    participant.Position,
                    occupiedPositions,
                    $"Battle '{battle.Id}' random participant");

                Ensure(participant.Tier >= 0,
                    $"Battle '{battle.Id}' random participant tier '{participant.Tier}' must be non-negative.");
                Ensure(participant.Boss || participant.Tier <= 3,
                    $"Battle '{battle.Id}' non-boss random participant tier '{participant.Tier}' must be between 0 and 3.");
                Ensure(participant.Team is 1 or 2,
                    $"Battle '{battle.Id}' random participant team '{participant.Team}' is unsupported.");
            }
        }
    }

    private static void ValidateBattlePosition(
        GridPosition position,
        ISet<GridPosition> occupiedPositions,
        string ownerName)
    {
        Ensure(position.X >= 0 && position.X < 13 && position.Y >= 0 && position.Y < 5,
            $"{ownerName} position ({position.X}, {position.Y}) exceeds supported battle grid size 13x5.");
        Ensure(occupiedPositions.Add(position),
            $"{ownerName} position ({position.X}, {position.Y}) overlaps another participant.");
    }

    private static void ValidateSkillAffixes(InMemoryContentRepository repository)
    {
        foreach (var skill in repository.ExternalSkills.Values)
        {
            foreach (var affix in skill.Affixes)
            {
                ValidateSkillAffix(affix, $"ExternalSkill '{skill.Id}'", repository);
            }
        }

        foreach (var skill in repository.InternalSkills.Values)
        {
            foreach (var affix in skill.Affixes)
            {
                ValidateSkillAffix(affix, $"InternalSkill '{skill.Id}'", repository);
            }
        }
    }

    private static void ValidateSkillAffix(
        SkillAffixDefinition affix,
        string ownerName,
        InMemoryContentRepository repository)
    {
        Ensure(affix.MinimumLevel >= 1, $"{ownerName} has skill affix with invalid minimum level '{affix.MinimumLevel}'.");
        Ensure(affix.Effect is not null, $"{ownerName} has skill affix without effect.");
        // The legacy XMJH data uses this gate on one external-skill talent
        // grant.  Keep the general schema guard for arbitrary external
        // effects, while allowing that explicitly representable legacy form.
        if (ownerName.StartsWith("ExternalSkill ", StringComparison.Ordinal) &&
            affix.RequiresEquippedInternalSkill)
        {
            Ensure(affix.Effect is GrantTalentAffix,
                $"{ownerName} cannot require an equipped internal skill for this effect type.");
        }
        Ensure(affix.Effect is not BuffLevelStatModifierAffix,
            $"{ownerName} cannot contain a buff-level modifier.");
        ValidateBattleHookAffix(affix.Effect!, ownerName, repository);
    }

    private static void ValidateBattleHookAffixes(InMemoryContentRepository repository)
    {
        foreach (var talent in repository.Talents.Values)
        {
            ValidateBattleHookAffixes(talent.Affixes, repository, $"Talent '{talent.Id}'");
            Ensure(talent.Affixes.All(static affix => affix is not BuffLevelStatModifierAffix),
                $"Talent '{talent.Id}' cannot contain a buff-level modifier.");
        }

        foreach (var buff in repository.Buffs.Values)
        {
            ValidateBattleHookAffixes(buff.Affixes, repository, $"Buff '{buff.Id}'");
            foreach (var affix in buff.Affixes)
            {
                Ensure(affix is BuffLevelStatModifierAffix or StatModifierAffix or SkillBonusModifierAffix or
                    WeaponBonusModifierAffix or SkillTargetingModifierAffix or TraitAffix or HookAffix,
                    $"Buff '{buff.Id}' contains unsupported affix '{affix.GetType().Name}'.");
            }
        }

        foreach (var equipment in repository.Equipments.Values)
        {
            ValidateBattleHookAffixes(equipment.Affixes, repository, $"Equipment '{equipment.Id}'");
            Ensure(equipment.Affixes.All(static affix => affix is not BuffLevelStatModifierAffix),
                $"Equipment '{equipment.Id}' cannot contain a buff-level modifier.");
        }
    }

    private static void ValidateBattleHookAffixes(
        IEnumerable<AffixDefinition> affixes,
        InMemoryContentRepository repository,
        string ownerName)
    {
        foreach (var affix in affixes)
        {
            ValidateBattleHookAffix(affix, ownerName, repository);
        }
    }

    private static void ValidateBattleHookAffix(
        AffixDefinition affix,
        string ownerName,
        InMemoryContentRepository? repository = null)
    {
        if (affix is not HookAffix hook)
        {
            return;
        }

        Ensure(hook.Effects.Count > 0 || hook.FloatText is not null || hook.Speech is not null,
            $"{ownerName} has battle hook '{hook.Timing}' without effects, float text, or speech.");
        ValidateBattleHookFloatText(hook, ownerName);
        ValidateBattleHookSpeech(hook, ownerName);

        foreach (var condition in hook.Conditions)
        {
            switch (condition)
            {
                case ChanceBattleHookConditionDefinition chance:
                    Ensure(chance.Value >= 0d && chance.Value <= 1d,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid chance '{chance.Value}'.");
                    break;
                case UnitLevelChanceBattleHookConditionDefinition chance:
                    Ensure(chance.BaseValue >= 0d && chance.BaseValue <= 1d,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid base chance '{chance.BaseValue}'.");
                    Ensure(chance.ValuePerLevel >= 0d,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid chance per level '{chance.ValuePerLevel}'.");
                    Ensure(chance.MaxValue >= 0d && chance.MaxValue <= 1d,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid maximum chance '{chance.MaxValue}'.");
                    Ensure(chance.BaseValue <= chance.MaxValue,
                        $"{ownerName} has battle hook '{hook.Timing}' with base chance greater than its maximum chance.");
                    break;
                case DamagePositiveBattleHookConditionDefinition:
                    break;
                case ContextBuffIdBattleHookConditionDefinition buffCondition:
                    Ensure(!string.IsNullOrWhiteSpace(buffCondition.BuffId),
                        $"{ownerName} has battle hook '{hook.Timing}' condition with empty buffId.");
                    if (repository is not null)
                    {
                        Ensure(repository.Buffs.ContainsKey(buffCondition.BuffId),
                            $"{ownerName} has battle hook '{hook.Timing}' condition referencing missing buff '{buffCondition.BuffId}'.");
                    }
                    break;
                case ContextBuffNegativeBattleHookConditionDefinition:
                    Ensure(hook.Timing is HookTiming.BeforeBuffApplied or HookTiming.OnBuffApplied or
                            HookTiming.OnBuffRemoved or HookTiming.AfterBuffRound,
                        $"{ownerName} has battle hook '{hook.Timing}' with a context buff condition outside a buff timing.");
                    break;
                case ContextUnitHpRatioBattleHookConditionDefinition hpRatioCondition:
                    Ensure(
                        hpRatioCondition.MinInclusive is not null ||
                        hpRatioCondition.MinExclusive is not null ||
                        hpRatioCondition.MaxExclusive is not null ||
                        hpRatioCondition.MaxInclusive is not null,
                        $"{ownerName} has battle hook '{hook.Timing}' hp ratio condition without bounds.");
                    if (hpRatioCondition.MinInclusive is { } minInclusive)
                    {
                        Ensure(minInclusive >= 0d && minInclusive <= 1d,
                            $"{ownerName} has battle hook '{hook.Timing}' hp ratio condition with invalid minInclusive '{minInclusive}'.");
                    }

                    if (hpRatioCondition.MinExclusive is { } minExclusive)
                    {
                        Ensure(minExclusive >= 0d && minExclusive <= 1d,
                            $"{ownerName} has battle hook '{hook.Timing}' hp ratio condition with invalid minExclusive '{minExclusive}'.");
                    }

                    if (hpRatioCondition.MaxExclusive is { } maxExclusive)
                    {
                        Ensure(maxExclusive >= 0d && maxExclusive <= 1d,
                            $"{ownerName} has battle hook '{hook.Timing}' hp ratio condition with invalid maxExclusive '{maxExclusive}'.");
                    }

                    if (hpRatioCondition.MaxInclusive is { } maxInclusive)
                    {
                        Ensure(maxInclusive >= 0d && maxInclusive <= 1d,
                            $"{ownerName} has battle hook '{hook.Timing}' hp ratio condition with invalid maxInclusive '{maxInclusive}'.");
                    }

                    var lowerBound = hpRatioCondition.MinInclusive ?? hpRatioCondition.MinExclusive;
                    var upperBound = hpRatioCondition.MaxExclusive ?? hpRatioCondition.MaxInclusive;
                    if (lowerBound is { } min && upperBound is { } max)
                    {
                        Ensure(min < max,
                            $"{ownerName} has battle hook '{hook.Timing}' hp ratio condition with invalid range '{min}'..'{max}'.");
                    }

                    break;
                case ContextUnitBuffBattleHookConditionDefinition unitBuffCondition:
                    Ensure(!string.IsNullOrWhiteSpace(unitBuffCondition.BuffId),
                        $"{ownerName} has battle hook '{hook.Timing}' unit buff condition with empty buff id.");
                    if (repository is not null)
                    {
                        Ensure(repository.Buffs.ContainsKey(unitBuffCondition.BuffId),
                            $"{ownerName} has battle hook '{hook.Timing}' unit buff condition referencing missing buff '{unitBuffCondition.BuffId}'.");
                    }
                    break;
                case ContextUnitEffectiveTalentBattleHookConditionDefinition talentCondition:
                    Ensure(talentCondition.TalentIds.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' effective talent condition without talentIds.");
                    foreach (var talentId in talentCondition.TalentIds)
                    {
                        Ensure(!string.IsNullOrWhiteSpace(talentId),
                            $"{ownerName} has battle hook '{hook.Timing}' effective talent condition with empty talent id.");
                        if (repository is not null)
                        {
                            Ensure(repository.Talents.ContainsKey(talentId),
                                $"{ownerName} has battle hook '{hook.Timing}' effective talent condition referencing missing talent '{talentId}'.");
                        }
                    }

                    break;
                case ContextUnitEquippedInternalSkillBattleHookConditionDefinition internalSkillCondition:
                    Ensure(internalSkillCondition.InternalSkillIds.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' equipped internal skill condition without internalSkillIds.");
                    if (repository is not null)
                    {
                        foreach (var internalSkillId in internalSkillCondition.InternalSkillIds)
                        {
                            Ensure(!string.IsNullOrWhiteSpace(internalSkillId),
                                $"{ownerName} has battle hook '{hook.Timing}' equipped internal skill condition with empty skill id.");
                            Ensure(repository.InternalSkills.ContainsKey(internalSkillId),
                                $"{ownerName} has battle hook '{hook.Timing}' equipped internal skill condition referencing missing internal skill '{internalSkillId}'.");
                        }
                    }
                    break;
                case ContextUnitRelationBattleHookConditionDefinition:
                    break;
                case ContextUnitGenderBattleHookConditionDefinition genderCondition:
                    Ensure(genderCondition.Genders.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' unit gender condition without genders.");
                    break;
                case ContextHitStateBattleHookConditionDefinition:
                    break;
                case ContextUnitRoleBattleHookConditionDefinition:
                    break;
                case ContextSkillSourceIdBattleHookConditionDefinition sourceSkillCondition:
                    Ensure(sourceSkillCondition.SourceSkillIds.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' source skill condition without sourceSkillIds.");
                    foreach (var sourceSkillId in sourceSkillCondition.SourceSkillIds)
                    {
                        Ensure(!string.IsNullOrWhiteSpace(sourceSkillId),
                            $"{ownerName} has battle hook '{hook.Timing}' source skill condition with empty source skill id.");
                        if (repository is not null)
                        {
                            Ensure(
                                repository.ExternalSkills.ContainsKey(sourceSkillId) ||
                                repository.InternalSkills.ContainsKey(sourceSkillId) ||
                                repository.SpecialSkills.ContainsKey(sourceSkillId),
                                $"{ownerName} has battle hook '{hook.Timing}' source skill condition referencing missing source skill '{sourceSkillId}'.");
                        }
                    }
                    break;
                case ContextSkillNameEqualsBattleHookConditionDefinition skillNameEqualsCondition:
                    Ensure(skillNameEqualsCondition.Values.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' skill name equals condition without values.");
                    foreach (var value in skillNameEqualsCondition.Values)
                    {
                        Ensure(!string.IsNullOrWhiteSpace(value),
                            $"{ownerName} has battle hook '{hook.Timing}' skill name equals condition with empty value.");
                    }
                    break;
                case ContextSkillNameContainsBattleHookConditionDefinition skillNameCondition:
                    Ensure(skillNameCondition.Values.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' skill name condition without values.");
                    foreach (var value in skillNameCondition.Values)
                    {
                        Ensure(!string.IsNullOrWhiteSpace(value),
                            $"{ownerName} has battle hook '{hook.Timing}' skill name condition with empty value.");
                    }
                    break;
                case ContextSkillKindBattleHookConditionDefinition skillKindCondition:
                    Ensure(skillKindCondition.Kinds.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' skill kind condition without kinds.");
                    break;
                case ContextSkillWeaponTypeBattleHookConditionDefinition skillWeaponTypeCondition:
                    Ensure(skillWeaponTypeCondition.WeaponTypes.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' skill weapon type condition without weaponTypes.");
                    foreach (var weaponType in skillWeaponTypeCondition.WeaponTypes)
                    {
                        Ensure(weaponType != WeaponType.Unknown,
                            $"{ownerName} has battle hook '{hook.Timing}' skill weapon type condition with unknown weapon type.");
                    }
                    break;
                case ContextRecoveryKindBattleHookConditionDefinition:
                    break;
                default:
                    throw new InvalidOperationException($"{ownerName} has unsupported battle hook condition '{condition.GetType().Name}'.");
            }
        }

        foreach (var effect in hook.Effects)
        {
            ValidateBattleHookEffectTiming(hook.Timing, effect, ownerName);
            switch (effect)
            {
                case ModifyDamageBattleHookEffectDefinition:
                case ModifyMpCostBattleHookEffectDefinition:
                case ModifyRecoveryBattleHookEffectDefinition:
                    break;
                case ModifyDamageContextBattleHookEffectDefinition modifyDamageContext:
                    ValidateModifyDamageContextEffect(modifyDamageContext, $"{ownerName} battle hook '{hook.Timing}'");
                    break;
                case ModifyLifestealBattleHookEffectDefinition modifyLifesteal:
                    Ensure(double.IsFinite(modifyLifesteal.Factor),
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid lifesteal factor '{modifyLifesteal.Factor}'.");
                    Ensure(double.IsFinite(modifyLifesteal.FactorPerUnitLevel),
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid lifesteal factor per unit level '{modifyLifesteal.FactorPerUnitLevel}'.");
                    break;
                case StrengthenContextBuffBattleHookEffectDefinition strengthenBuff:
                    Ensure(strengthenBuff.LevelDelta >= 0,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid buff level delta '{strengthenBuff.LevelDelta}'.");
                    Ensure(strengthenBuff.TurnDelta >= 0,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid buff turn delta '{strengthenBuff.TurnDelta}'.");
                    break;
                case ApplyBuffBattleEffectDefinition:
                case RemoveBuffBattleEffectDefinition:
                case RemoveNegativeBuffsBattleEffectDefinition:
                case RemovePositiveBuffsBattleEffectDefinition:
                case RemoveContextBuffBattleEffectDefinition:
                case AddRageBattleEffectDefinition:
                case SetRageBattleEffectDefinition:
                case AddActionGaugeBattleEffectDefinition:
                case SetActionGaugeBattleEffectDefinition:
                case AddHpBattleEffectDefinition:
                case AddMpBattleEffectDefinition:
                case CancelHitBattleHookEffectDefinition:
                case SetHitSuccessBattleHookEffectDefinition:
                case ExtraStrikeBattleHookEffectDefinition:
                    ValidateSharedBattleEffect(effect, $"{ownerName} battle hook '{hook.Timing}'", repository);
                    break;
                case CustomBattleEffectDefinition:
                    break;
                case GrantScopedBattleEffectDefinition grant:
                    Ensure(hook.Timing == HookTiming.OnBattleStart,
                        $"{ownerName} can only grant scoped effects during '{HookTiming.OnBattleStart}'.");
                    Ensure(repository is not null && repository.ScopedBattleEffects.ContainsKey(grant.EffectId),
                        $"{ownerName} references missing scoped battle effect '{grant.EffectId}'.");
                    break;
                default:
                    throw new InvalidOperationException($"{ownerName} has unsupported battle hook effect '{effect.GetType().Name}'.");
            }
        }
    }

    private static void ValidateBattleHookEffectTiming(
        HookTiming timing,
        BattleEffectDefinition effect,
        string ownerName)
    {
        var supported = BattleEffectTimingPolicy.Supports(timing, effect);

        Ensure(supported,
            $"{ownerName} battle hook '{timing}' does not support effect '{effect.GetType().Name}'.");
    }

    private static void ValidateModifyDamageContextEffect(
        ModifyDamageContextBattleHookEffectDefinition effect,
        string ownerName)
    {
        var hasRange = effect.DeltaMin is not null || effect.DeltaMax is not null;
        if (effect.DeltaPowerBasePerBuffLevel is { } powerBase)
        {
            Ensure(powerBase > 0d,
                $"{ownerName} modify_damage_context effect has invalid deltaPowerBasePerBuffLevel '{powerBase}'.");
        }

        if (!hasRange)
        {
            return;
        }

        Ensure(effect.DeltaMin is not null && effect.DeltaMax is not null,
            $"{ownerName} modify_damage_context effect must provide both deltaMin and deltaMax.");
        Ensure(Math.Abs(effect.Delta) <= double.Epsilon,
            $"{ownerName} modify_damage_context effect cannot combine delta with deltaMin/deltaMax.");
        Ensure(effect.DeltaMin <= effect.DeltaMax,
            $"{ownerName} modify_damage_context effect has invalid range '{effect.DeltaMin}'..'{effect.DeltaMax}'.");
    }

    private static void ValidateSharedBattleEffect(
        BattleEffectDefinition effect,
        string ownerName,
        InMemoryContentRepository? repository)
    {
        switch (effect)
        {
            case ApplyBuffBattleEffectDefinition applyBuff:
                Ensure(applyBuff.Target is not null, $"{ownerName} apply_buff effect is missing target.");
                Ensure(!string.IsNullOrWhiteSpace(applyBuff.BuffId), $"{ownerName} apply_buff effect is missing buffId.");
                if (repository is not null)
                {
                    Ensure(repository.Buffs.ContainsKey(applyBuff.BuffId), $"{ownerName} references missing buff '{applyBuff.BuffId}'.");
                }
                Ensure(applyBuff.Level >= 0, $"{ownerName} apply_buff effect has invalid level '{applyBuff.Level}'.");
                Ensure(applyBuff.Duration >= 1, $"{ownerName} apply_buff effect has invalid duration '{applyBuff.Duration}'.");
                Ensure(applyBuff.Chance is >= 0 and <= 100, $"{ownerName} apply_buff effect has invalid chance '{applyBuff.Chance}'.");
                ValidateBattleUnitSelector(applyBuff.Target!, ownerName, null);
                break;
            case RemoveBuffBattleEffectDefinition removeBuff:
                Ensure(removeBuff.Target is not null, $"{ownerName} remove_buff effect is missing target.");
                Ensure(!string.IsNullOrWhiteSpace(removeBuff.BuffId), $"{ownerName} remove_buff effect is missing buffId.");
                if (repository is not null)
                {
                    Ensure(repository.Buffs.ContainsKey(removeBuff.BuffId), $"{ownerName} references missing buff '{removeBuff.BuffId}'.");
                }
                ValidateBattleUnitSelector(removeBuff.Target!, ownerName, null);
                break;
            case RemoveNegativeBuffsBattleEffectDefinition removeNegativeBuffs:
                Ensure(removeNegativeBuffs.Target is not null, $"{ownerName} remove_negative_buffs effect is missing target.");
                ValidateBattleUnitSelector(removeNegativeBuffs.Target!, ownerName, null);
                break;
            case RemovePositiveBuffsBattleEffectDefinition removePositiveBuffs:
                Ensure(removePositiveBuffs.Target is not null, $"{ownerName} remove_positive_buffs effect is missing target.");
                ValidateBattleUnitSelector(removePositiveBuffs.Target!, ownerName, null);
                break;
            case RemoveContextBuffBattleEffectDefinition:
                break;
            case AddRageBattleEffectDefinition addRage:
                Ensure(addRage.Target is not null, $"{ownerName} add_rage effect is missing target.");
                Ensure(addRage.Value >= 0, $"{ownerName} add_rage effect has invalid value '{addRage.Value}'.");
                ValidateBattleUnitSelector(addRage.Target!, ownerName, null);
                break;
            case SetRageBattleEffectDefinition setRage:
                Ensure(setRage.Target is not null, $"{ownerName} set_rage effect is missing target.");
                Ensure(setRage.Value >= 0 && setRage.Value <= BattleUnit.MaxRage,
                    $"{ownerName} set_rage effect has invalid value '{setRage.Value}'.");
                ValidateBattleUnitSelector(setRage.Target!, ownerName, null);
                break;
            case AddActionGaugeBattleEffectDefinition addActionGauge:
                Ensure(addActionGauge.Target is not null, $"{ownerName} add_action_gauge effect is missing target.");
                ValidateBattleUnitSelector(addActionGauge.Target!, ownerName, null);
                break;
            case SetActionGaugeBattleEffectDefinition setActionGauge:
                Ensure(setActionGauge.Target is not null, $"{ownerName} set_action_gauge effect is missing target.");
                Ensure(setActionGauge.Value >= 0, $"{ownerName} set_action_gauge effect has invalid value '{setActionGauge.Value}'.");
                ValidateBattleUnitSelector(setActionGauge.Target!, ownerName, null);
                break;
            case AddHpBattleEffectDefinition addHp:
                Ensure(addHp.Target is not null, $"{ownerName} add_hp effect is missing target.");
                Ensure(addHp.Value >= 0, $"{ownerName} add_hp effect has invalid value '{addHp.Value}'.");
                ValidateBattleUnitSelector(addHp.Target!, ownerName, null);
                break;
            case AddMpBattleEffectDefinition addMp:
                Ensure(addMp.Target is not null, $"{ownerName} add_mp effect is missing target.");
                Ensure(addMp.Value >= 0, $"{ownerName} add_mp effect has invalid value '{addMp.Value}'.");
                ValidateBattleUnitSelector(addMp.Target!, ownerName, null);
                break;
            case CancelHitBattleHookEffectDefinition:
            case SetHitSuccessBattleHookEffectDefinition:
                break;
            case ExtraStrikeBattleHookEffectDefinition extraStrike:
                Ensure(extraStrike.Target is not null, $"{ownerName} extra_strike effect is missing target.");
                Ensure(extraStrike.Chance >= 0d && extraStrike.Chance <= 100d,
                    $"{ownerName} extra_strike effect has invalid chance '{extraStrike.Chance}'.");
                Ensure(extraStrike.ChancePerBuffLevel >= 0d,
                    $"{ownerName} extra_strike effect has invalid chancePerBuffLevel '{extraStrike.ChancePerBuffLevel}'.");
                var damageFactors = extraStrike.DamageFactors;
                Ensure(damageFactors is { Count: > 0 },
                    $"{ownerName} extra_strike effect must provide at least one damage factor.");
                foreach (var damageFactor in damageFactors)
                {
                    Ensure(damageFactor > 0d,
                        $"{ownerName} extra_strike effect has invalid damage factor '{damageFactor}'.");
                }
                ValidateBattleUnitSelector(extraStrike.Target!, ownerName, null);
                break;
            default:
                throw new InvalidOperationException($"{ownerName} has unsupported shared battle effect '{effect.GetType().Name}'.");
        }
    }

    private static void ValidateBattleHookSpeech(HookAffix hook, string ownerName)
    {
        if (hook.Speech is null)
        {
            return;
        }

        ValidateBattleSpeech(hook.Speech, $"{ownerName} battle hook '{hook.Timing}'");
    }

    private static void ValidateBattleHookFloatText(HookAffix hook, string ownerName)
    {
        if (hook.FloatText is null)
        {
            return;
        }

        Ensure(!string.IsNullOrWhiteSpace(hook.FloatText.Text),
            $"{ownerName} battle hook '{hook.Timing}' has empty float text.");
    }

    private static void ValidateBattleSpeech(BattleSpeechDefinition speech, string ownerName)
    {
        Ensure(speech.Lines.Count > 0,
            $"{ownerName} speech without lines.");
        Ensure(speech.Chance >= 0d && speech.Chance <= 1d,
            $"{ownerName} speech with invalid chance '{speech.Chance}'.");

        foreach (var line in speech.Lines)
        {
            Ensure(!string.IsNullOrWhiteSpace(line),
                $"{ownerName} speech with empty line.");
        }
    }

    private static void ValidateBattleUnitSelector(
        BattleUnitSelectorDefinition selector,
        string ownerName,
        HookTiming? timing)
    {
        var scope = timing is null ? ownerName : $"{ownerName} '{timing}'";
        switch (selector)
        {
            case SelfBattleUnitSelectorDefinition:
            case SourceBattleUnitSelectorDefinition:
            case TargetBattleUnitSelectorDefinition:
            case AllUnitsBattleUnitSelectorDefinition:
            case AllAlliesBattleUnitSelectorDefinition:
            case AllEnemiesBattleUnitSelectorDefinition:
                break;
            case NearbyAlliesBattleUnitSelectorDefinition nearbyAllies:
                Ensure(nearbyAllies.Radius >= 0,
                    $"{scope} nearby_allies selector has invalid radius '{nearbyAllies.Radius}'.");
                break;
            case NearbyEnemiesBattleUnitSelectorDefinition nearbyEnemies:
                Ensure(nearbyEnemies.Radius >= 0,
                    $"{scope} nearby_enemies selector has invalid radius '{nearbyEnemies.Radius}'.");
                break;
            case ExplicitUnitsBattleUnitSelectorDefinition:
                throw new InvalidOperationException($"{scope} cannot use explicit_units outside a scoped effect.");
            default:
                throw new InvalidOperationException($"{scope} has unsupported battle target selector '{selector.GetType().Name}'.");
        }
    }

    private static void ValidateItemReferences(InMemoryContentRepository repository)
    {
        var buffIds = repository.Buffs.Keys.ToHashSet(StringComparer.Ordinal);
        var externalSkillIds = repository.ExternalSkills.Keys.ToHashSet(StringComparer.Ordinal);
        var internalSkillIds = repository.InternalSkills.Keys.ToHashSet(StringComparer.Ordinal);
        var specialSkillIds = repository.SpecialSkills.Keys.ToHashSet(StringComparer.Ordinal);
        var talentIds = repository.Talents.Keys.ToHashSet(StringComparer.Ordinal);
        var characterTitleIds = repository.CharacterTitles.Keys.ToHashSet(StringComparer.Ordinal);
        var itemIds = repository.Items.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var item in repository.Items.Values)
        {
            foreach (var requirement in item.Requirements ?? [])
            {
                switch (requirement)
                {
                    case TalentItemRequirementDefinition talentRequirement:
                        Ensure(!string.IsNullOrWhiteSpace(talentRequirement.TalentId), $"Item '{item.Id}' talent requirement is missing talentId.");
                        Ensure(talentIds.Contains(talentRequirement.TalentId), $"Item '{item.Id}' references missing talent '{talentRequirement.TalentId}'.");
                        break;

                    case StatItemRequirementDefinition:
                        break;

                    case LevelItemRequirementDefinition levelRequirement:
                        Ensure(levelRequirement.Value >= 0,
                            $"Item '{item.Id}' level requirement has invalid value '{levelRequirement.Value}'.");
                        break;

                    case GenderItemRequirementDefinition genderRequirement:
                        Ensure(genderRequirement.Genders is { Count: > 0 },
                            $"Item '{item.Id}' gender requirement has no allowed genders.");
                        Ensure(genderRequirement.Genders!.All(Enum.IsDefined),
                            $"Item '{item.Id}' gender requirement contains an invalid gender.");
                        Ensure(genderRequirement.Genders.Distinct().Count() == genderRequirement.Genders.Count,
                            $"Item '{item.Id}' gender requirement contains duplicate genders.");
                        break;
                }
            }

            if (item is EquipmentDefinition equipment)
            {
                foreach (var grantedSkill in equipment.GrantedSkills ?? [])
                {
                    Ensure(!string.IsNullOrWhiteSpace(grantedSkill.SkillId),
                        $"Item '{item.Id}' granted skill is missing skillId.");
                    Ensure(externalSkillIds.Contains(grantedSkill.SkillId),
                        $"Item '{item.Id}' references missing external skill '{grantedSkill.SkillId}'.");
                    Ensure(grantedSkill.Level >= 1,
                        $"Item '{item.Id}' granted skill '{grantedSkill.SkillId}' has invalid level '{grantedSkill.Level}'.");
                }

                foreach (var grantedSpecial in equipment.GrantedSpecialSkills ?? [])
                {
                    Ensure(!string.IsNullOrWhiteSpace(grantedSpecial.SkillId),
                        $"Item '{item.Id}' granted special skill is missing skillId.");
                    Ensure(specialSkillIds.Contains(grantedSpecial.SkillId),
                        $"Item '{item.Id}' references missing special skill '{grantedSpecial.SkillId}'.");
                }
            }

            foreach (var useEffect in item.UseEffects ?? [])
            {
                switch (useEffect)
                {
                    case AddBuffItemUseEffectDefinition addBuff:
                        Ensure(!string.IsNullOrWhiteSpace(addBuff.BuffId), $"Item '{item.Id}' add_buff effect is missing buffId.");
                        Ensure(buffIds.Contains(addBuff.BuffId), $"Item '{item.Id}' references missing buff '{addBuff.BuffId}'.");
                        Ensure(addBuff.Level >= 0, $"Item '{item.Id}' add_buff effect has invalid level '{addBuff.Level}'.");
                        Ensure(addBuff.Duration >= 1, $"Item '{item.Id}' add_buff effect has invalid duration '{addBuff.Duration}'.");
                        break;

                    case DetoxifyItemUseEffectDefinition detoxify:
                        Ensure(detoxify.Values is { Count: 2 },
                            $"Item '{item.Id}' detoxify effect must contain exactly two values.");
                        Ensure(detoxify.Values!.All(static value => value >= 0),
                            $"Item '{item.Id}' detoxify effect contains a negative reduction.");
                        Ensure(detoxify.Values!.Any(static value => value > 0),
                            $"Item '{item.Id}' detoxify effect must contain a positive reduction.");
                        break;

                    case GrantExternalSkillItemUseEffectDefinition externalSkill:
                        Ensure(!string.IsNullOrWhiteSpace(externalSkill.SkillId), $"Item '{item.Id}' external_skill effect is missing skillId.");
                        Ensure(externalSkillIds.Contains(externalSkill.SkillId), $"Item '{item.Id}' references missing external skill '{externalSkill.SkillId}'.");
                        Ensure(externalSkill.Level is null or >= 1, $"Item '{item.Id}' external_skill effect has invalid level '{externalSkill.Level}'.");
                        break;

                    case GrantInternalSkillItemUseEffectDefinition internalSkill:
                        Ensure(!string.IsNullOrWhiteSpace(internalSkill.SkillId), $"Item '{item.Id}' internal_skill effect is missing skillId.");
                        Ensure(internalSkillIds.Contains(internalSkill.SkillId), $"Item '{item.Id}' references missing internal skill '{internalSkill.SkillId}'.");
                        Ensure(internalSkill.Level is null or >= 1, $"Item '{item.Id}' internal_skill effect has invalid level '{internalSkill.Level}'.");
                        break;

                    case GrantSpecialSkillItemUseEffectDefinition specialSkill:
                        Ensure(!string.IsNullOrWhiteSpace(specialSkill.SkillId), $"Item '{item.Id}' special_skill effect is missing skillId.");
                        Ensure(specialSkillIds.Contains(specialSkill.SkillId), $"Item '{item.Id}' references missing special skill '{specialSkill.SkillId}'.");
                        break;

                    case GrantTalentItemUseEffectDefinition talent:
                        Ensure(!string.IsNullOrWhiteSpace(talent.TalentId), $"Item '{item.Id}' grant_talent effect is missing talentId.");
                        Ensure(talentIds.Contains(talent.TalentId), $"Item '{item.Id}' references missing talent '{talent.TalentId}'.");
                        break;

                    case SetGenderItemUseEffectDefinition setGender:
                        Ensure(Enum.IsDefined(setGender.Gender),
                            $"Item '{item.Id}' set_gender effect has invalid gender '{setGender.Gender}'.");
                        break;

                    case ReduceMaxResourceRatioItemUseEffectDefinition reduction:
                        Ensure(reduction.StatId is StatType.MaxHp or StatType.MaxMp,
                            $"Item '{item.Id}' reduce_max_resource_ratio effect has unsupported stat '{reduction.StatId}'.");
                        Ensure(double.IsFinite(reduction.Ratio) && reduction.Ratio > 0d && reduction.Ratio < 1d,
                            $"Item '{item.Id}' reduce_max_resource_ratio effect has invalid ratio '{reduction.Ratio}'.");
                        break;

                    case AddStatsItemUseEffectDefinition addStats:
                        Ensure(addStats.Values is { Count: > 0 },
                            $"Item '{item.Id}' add_stats effect has no values.");
                        Ensure(addStats.Values!.All(static entry => Enum.IsDefined(entry.Key)),
                            $"Item '{item.Id}' add_stats effect contains an invalid stat.");
                        Ensure(addStats.Values.All(static entry => entry.Value != 0),
                            $"Item '{item.Id}' add_stats effect contains a zero change.");
                        break;

                    case RunStoryItemUseEffectDefinition runStory:
                        Ensure(!string.IsNullOrWhiteSpace(runStory.StoryId),
                            $"Item '{item.Id}' run_story effect is missing storyId.");
                        Ensure(repository.StorySegments.ContainsKey(runStory.StoryId),
                            $"Item '{item.Id}' references missing story segment '{runStory.StoryId}'.");
                        Ensure(item is NormalItemDefinition &&
                               item.Type is ItemType.SkillBook or ItemType.SpecialSkillBook or
                                   ItemType.TalentBook or ItemType.Booster or ItemType.Utility or
                                   ItemType.QuestItem,
                            $"Item '{item.Id}' run_story effect is only supported by out-of-battle normal items.");
                        Ensure(item.UseEffects is { Count: 1 },
                            $"Item '{item.Id}' run_story effect must be the item's only use effect.");
                        break;

                    case GrantTitleItemUseEffectDefinition grantTitle:
                        Ensure(!string.IsNullOrWhiteSpace(grantTitle.TitleId),
                            $"Item '{item.Id}' grant_title effect is missing titleId.");
                        Ensure(characterTitleIds.Contains(grantTitle.TitleId),
                            $"Item '{item.Id}' references missing character title '{grantTitle.TitleId}'.");
                        break;

                    case SetPortraitItemUseEffectDefinition setPortrait:
                        Ensure(!string.IsNullOrWhiteSpace(setPortrait.PictureId),
                            $"Item '{item.Id}' set_portrait effect is missing pictureId.");
                        Ensure(repository.TryGetResource(setPortrait.PictureId, out _),
                            $"Item '{item.Id}' references missing portrait resource '{setPortrait.PictureId}'.");
                        break;

                    case RandomItemItemUseEffectDefinition randomItem:
                        Ensure(randomItem.Items is { Count: > 0 },
                            $"Item '{item.Id}' random_item effect has no entries.");
                        Ensure(randomItem.Items!.All(entry =>
                                !string.IsNullOrWhiteSpace(entry.ItemId) &&
                                entry.Quantity >= 1 &&
                                itemIds.Contains(entry.ItemId)),
                            $"Item '{item.Id}' random_item effect references a missing item or an invalid quantity.");
                        break;
                }
            }

            if (item is not EquipmentDefinition &&
                item.Type is not ItemType.Consumable &&
                item.UseEffects is { Count: > 0 })
            {
                Ensure(item.UseEffects.All(IsSupportedOutOfBattleItemEffect),
                    $"Item '{item.Id}' contains an item effect that is not supported outside battle.");
            }
        }
    }

    private static void ValidateScopedBattleEffects(InMemoryContentRepository repository)
    {
        foreach (var definition in repository.ScopedBattleEffects.Values)
        {
            Ensure(!string.IsNullOrWhiteSpace(definition.Id), "Scoped battle effect has empty id.");
            Ensure(definition.Scope is not null, $"Scoped battle effect '{definition.Id}' has no scope.");
            Ensure(definition.RequiredMembers >= 1,
                $"Scoped battle effect '{definition.Id}' has invalid requiredMembers '{definition.RequiredMembers}'.");
            if (definition.GrantMode == ScopedBattleEffectGrantMode.PerProvider)
            {
                Ensure(definition.RequiredMembers == 1,
                    $"Scoped battle effect '{definition.Id}' with per_provider grant mode must require exactly one member.");
                Ensure(definition.Scope is not ExplicitUnitsBattleUnitSelectorDefinition,
                    $"Scoped battle effect '{definition.Id}' cannot use explicit_units with per_provider grant mode.");
                Ensure(definition.Lifetime != BattleEffectLifetime.RemoveWhenMemberDefeated,
                    $"Scoped battle effect '{definition.Id}' cannot use remove_when_member_defeated with per_provider grant mode.");
            }
            else
            {
                Ensure(definition.Activation != BattleEffectActivation.SourceAlive,
                    $"Scoped battle effect '{definition.Id}' cannot use source_alive with per_team_group grant mode.");
            }
            ValidateScopedSelector(definition.Scope!, definition.Id);
            foreach (var affix in definition.Affixes)
            {
                Ensure(affix is StatModifierAffix or SkillBonusModifierAffix or WeaponBonusModifierAffix or
                    SkillTargetingModifierAffix or TraitAffix or HookAffix,
                    $"Scoped battle effect '{definition.Id}' contains unsupported affix '{affix.GetType().Name}'.");
                if (affix is StatModifierAffix stat)
                    Ensure(stat.Stat is not StatType.MaxHp and not StatType.MaxMp,
                        $"Scoped battle effect '{definition.Id}' cannot modify '{stat.Stat}'.");
                if (affix is HookAffix hook)
                {
                    Ensure(hook.Effects.All(static effect => effect is not GrantScopedBattleEffectDefinition),
                        $"Scoped battle effect '{definition.Id}' cannot recursively grant scoped effects.");
                    ValidateBattleHookAffix(hook, $"Scoped battle effect '{definition.Id}'", repository);
                }
            }
        }
    }

    private static void ValidateScopedSelector(BattleUnitSelectorDefinition selector, string effectId)
    {
        switch (selector)
        {
            case AllUnitsBattleUnitSelectorDefinition:
            case AllAlliesBattleUnitSelectorDefinition:
            case AllEnemiesBattleUnitSelectorDefinition:
            case ExplicitUnitsBattleUnitSelectorDefinition:
                break;
            case NearbyAlliesBattleUnitSelectorDefinition nearby:
                Ensure(nearby.Radius >= 0, $"Scoped battle effect '{effectId}' has invalid radius '{nearby.Radius}'.");
                break;
            case NearbyEnemiesBattleUnitSelectorDefinition nearby:
                Ensure(nearby.Radius >= 0, $"Scoped battle effect '{effectId}' has invalid radius '{nearby.Radius}'.");
                break;
            default:
                throw new InvalidOperationException(
                    $"Scoped battle effect '{effectId}' uses unstable selector '{selector.GetType().Name}'.");
        }
    }

    private static bool IsSupportedOutOfBattleItemEffect(ItemUseEffectDefinition effect) =>
        effect is GrantExternalSkillItemUseEffectDefinition or
            GrantInternalSkillItemUseEffectDefinition or
            GrantSpecialSkillItemUseEffectDefinition or
            GrantTalentItemUseEffectDefinition or
            GrantTitleItemUseEffectDefinition or
            SetPortraitItemUseEffectDefinition or
            RandomItemItemUseEffectDefinition or
            AddStatsItemUseEffectDefinition or
            SetGenderItemUseEffectDefinition or
            ReduceMaxResourceRatioItemUseEffectDefinition or
            RunStoryItemUseEffectDefinition;

    private static void ValidateShops(InMemoryContentRepository repository)
    {
        foreach (var shop in repository.Shops.Values)
        {
            var productIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < shop.Products.Count; index += 1)
            {
                var product = shop.Products[index];
                Ensure(!string.IsNullOrWhiteSpace(product.Id),
                    $"Shop '{shop.Id}' product {index} has empty id.");
                Ensure(productIds.Add(product.Id),
                    $"Shop '{shop.Id}' has duplicate product id '{product.Id}'.");
                var reward = product.Reward ?? throw new InvalidOperationException(
                    $"Shop '{shop.Id}' product {index} is missing reward.");
                ValidateReward(repository, reward, $"Shop '{shop.Id}' product {index}");
                Ensure(product.MaxClaims is null or > 0, $"Shop '{shop.Id}' product {index} has invalid maxClaims.");
                Ensure(product.Price is null or >= 0, $"Shop '{shop.Id}' product {index} has invalid price.");
                Ensure(product.PremiumPrice is null or >= 0, $"Shop '{shop.Id}' product {index} has invalid premiumPrice.");
                if (reward is YuanbaoRewardDefinition)
                {
                    Ensure(product.Price is not null && product.PremiumPrice is null,
                        $"Shop '{shop.Id}' product {index} yuanbao reward must be purchased with silver.");
                }
                else if (reward is not ItemRewardDefinition)
                {
                    Ensure(product.Price is not null || product.PremiumPrice is not null,
                        $"Shop '{shop.Id}' product {index} special reward requires an explicit price.");
                }
            }
        }
    }

    private static void ValidateTowers(InMemoryContentRepository repository)
    {
        foreach (var tower in repository.Towers.Values)
        {
            Ensure(!string.IsNullOrWhiteSpace(tower.Id), "Tower definition has empty id.");

            foreach (var stage in tower.Stages)
            {
                Ensure(!string.IsNullOrWhiteSpace(stage.Id), $"Tower '{tower.Id}' has a stage with empty id.");
                Ensure(!string.IsNullOrWhiteSpace(stage.BattleId), $"Tower '{tower.Id}' stage '{stage.Id}' has empty battleId.");
                Ensure(repository.Battles.ContainsKey(stage.BattleId),
                    $"Tower '{tower.Id}' stage '{stage.Id}' references missing battle '{stage.BattleId}'.");

                var rewardIds = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < stage.Rewards.Count; index += 1)
                {
                    var reward = stage.Rewards[index];
                    Ensure(!string.IsNullOrWhiteSpace(reward.Id),
                        $"Tower '{tower.Id}' stage '{stage.Id}' reward {index} has empty id.");
                    Ensure(rewardIds.Add(reward.Id),
                        $"Tower '{tower.Id}' stage '{stage.Id}' has duplicate reward id '{reward.Id}'.");
                    var definition = reward.Reward ?? throw new InvalidOperationException(
                        $"Tower '{tower.Id}' stage '{stage.Id}' has an empty reward.");
                    ValidateReward(repository, definition,
                        $"Tower '{tower.Id}' stage '{stage.Id}' reward");
                    Ensure(double.IsFinite(reward.Weight) && reward.Weight > 0d,
                        $"Tower '{tower.Id}' stage '{stage.Id}' reward has invalid weight '{reward.Weight}'.");
                    Ensure(reward.MaxClaims is null or > 0,
                        $"Tower '{tower.Id}' stage '{stage.Id}' reward has invalid maxClaims '{reward.MaxClaims}'.");
                }

                foreach (var achievementId in stage.AchievementIds)
                {
                    Ensure(!string.IsNullOrWhiteSpace(achievementId),
                        $"Tower '{tower.Id}' stage '{stage.Id}' has empty achievement id.");

                    var resourceId = AchievementResourcePrefix + achievementId;
                    if (!repository.Resources.TryGetValue(resourceId, out var resource))
                    {
                        throw new InvalidOperationException(
                            $"Tower '{tower.Id}' stage '{stage.Id}' references missing achievement resource '{resourceId}'.");
                    }

                    Ensure(string.Equals(resource.Group, AchievementResourceGroup, StringComparison.Ordinal),
                        $"Tower '{tower.Id}' stage '{stage.Id}' achievement '{achievementId}' must resolve to a '{AchievementResourceGroup}' resource.");
                }
            }
        }
    }

    private static void ValidateReward(
        InMemoryContentRepository repository,
        RewardDefinition reward,
        string context)
    {
        Ensure(reward.Quantity > 0, $"{context} has invalid reward quantity '{reward.Quantity}'.");

        switch (reward)
        {
            case ItemRewardDefinition item:
                Ensure(!string.IsNullOrWhiteSpace(item.ItemId), $"{context} has an empty itemId.");
                Ensure(repository.Items.ContainsKey(item.ItemId),
                    $"{context} references missing item '{item.ItemId}'.");
                return;
            case YuanbaoRewardDefinition:
                return;
            case SkillMaxLevelRewardDefinition fragment:
                Ensure(!string.IsNullOrWhiteSpace(fragment.SkillId), $"{context} has an empty skillId.");
                Ensure(
                    fragment.SkillKind switch
                    {
                        SkillFragmentKind.External => repository.ExternalSkills.ContainsKey(fragment.SkillId),
                        SkillFragmentKind.Internal => repository.InternalSkills.ContainsKey(fragment.SkillId),
                        _ => false,
                    },
                    $"{context} references missing {fragment.SkillKind} skill '{fragment.SkillId}'.");
                return;
            default:
                throw new InvalidOperationException($"{context} uses unsupported reward type '{reward.GetType().Name}'.");
        }
    }

    private static void ValidateLegendSkills(InMemoryContentRepository repository)
    {
        var skillIds = repository.ExternalSkills.Keys
            .Concat(repository.ExternalSkills.Values.SelectMany(skill => skill.FormSkills.Select(form => form.Id)))
            .Concat(repository.InternalSkills.Keys)
            .Concat(repository.InternalSkills.Values.SelectMany(skill => skill.FormSkills.Select(form => form.Id)))
            .ToHashSet(StringComparer.Ordinal);
        var externalSkillIds = repository.ExternalSkills.Keys.ToHashSet(StringComparer.Ordinal);
        var internalSkillIds = repository.InternalSkills.Keys.ToHashSet(StringComparer.Ordinal);
        var specialSkillIds = repository.SpecialSkills.Keys.ToHashSet(StringComparer.Ordinal);
        var talentIds = repository.Talents.Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var legend in repository.LegendSkills)
        {
            Ensure(skillIds.Contains(legend.StartSkill), $"LegendSkill '{legend.Id}' references missing start skill '{legend.StartSkill}'.");
            Ensure(legend.Probability >= 0d && legend.Probability <= 1d, $"LegendSkill '{legend.Id}' has invalid probability '{legend.Probability}'.");
            Ensure(legend.RequiredLevel >= 1, $"LegendSkill '{legend.Id}' has invalid minimum level '{legend.RequiredLevel}'.");

            foreach (var condition in legend.Conditions)
            {
                switch (condition)
                {
                    case RequiredExternalSkillLevelLegendConditionDefinition externalSkill:
                        Ensure(externalSkillIds.Contains(externalSkill.TargetId), $"LegendSkill '{legend.Id}' references missing external skill '{externalSkill.TargetId}'.");
                        Ensure(externalSkill.Level >= 0, $"LegendSkill '{legend.Id}' has invalid external skill requirement level '{externalSkill.Level}'.");
                        break;
                    case RequiredInternalSkillLevelLegendConditionDefinition internalSkill:
                        Ensure(internalSkillIds.Contains(internalSkill.TargetId), $"LegendSkill '{legend.Id}' references missing internal skill '{internalSkill.TargetId}'.");
                        Ensure(internalSkill.Level >= 0, $"LegendSkill '{legend.Id}' has invalid internal skill requirement level '{internalSkill.Level}'.");
                        break;
                    case RequiredSpecialSkillLegendConditionDefinition specialSkill:
                        Ensure(specialSkillIds.Contains(specialSkill.TargetId), $"LegendSkill '{legend.Id}' references missing special skill '{specialSkill.TargetId}'.");
                        break;
                    case RequiredTalentLegendConditionDefinition talent:
                        Ensure(talentIds.Contains(talent.TargetId), $"LegendSkill '{legend.Id}' references missing talent '{talent.TargetId}'.");
                        break;
                }
            }
        }
    }

    private static void ValidateStoryContent(InMemoryContentRepository repository)
    {
        ValidateStoryScripts(repository);
        ValidateMapStoryReferences(repository);
    }

    private static void ValidateStoryScripts(InMemoryContentRepository repository)
    {
        foreach (var entry in repository.StorySegments.Values)
        {
            ValidateStorySteps(entry.Segment.Steps, repository, $"Story segment '{entry.Id}'");
        }
    }

    private static void ValidateStorySteps(
        IReadOnlyList<Step> steps,
        InMemoryContentRepository repository,
        string ownerName)
    {
        foreach (var step in steps)
        {
            switch (step)
            {
                case DialogueStep:
                case SetVariableStep:
                case DeleteVariableStep:
                    break;
                case CommandStep command:
                    ValidateStoryMediaCommand(command.Call, repository, ownerName);
                    ValidateMapCommandReference(repository, command.Call, ownerName);
                    break;
                case JumpStep jump:
                    Ensure(repository.StorySegments.ContainsKey(jump.Target),
                        $"{ownerName} jumps to missing story segment '{jump.Target}'.");
                    break;
                case CallStep call:
                    Ensure(repository.StorySegments.ContainsKey(call.Target),
                        $"{ownerName} calls missing story segment '{call.Target}'.");
                    break;
                case ReturnStep:
                    break;
                case ChoiceStep choice:
                    Ensure(choice.Blocks.Count > 0, $"{ownerName} has choice without blocks.");
                    foreach (var block in choice.Blocks)
                    {
                        switch (block)
                        {
                            case ChoiceOptionsBlock optionsBlock:
                                ValidateChoiceOptions(optionsBlock.Options, repository, ownerName);
                                break;
                            case ChoiceBranchBlock branchBlock:
                                Ensure(branchBlock.Cases.Count > 0, $"{ownerName} has choice branch without cases.");
                                foreach (var branchCase in branchBlock.Cases)
                                {
                                    ValidateChoiceOptions(branchCase.Options, repository, ownerName);
                                }

                                if (branchBlock.Fallback is not null)
                                {
                                    ValidateChoiceOptions(branchBlock.Fallback, repository, ownerName);
                                }

                                break;
                            default:
                                throw new InvalidOperationException($"Unsupported choice block type '{block.GetType().Name}'.");
                        }
                    }

                    break;
                case BattleStep battle:
                    Ensure(repository.Battles.ContainsKey(battle.BattleId),
                        $"{ownerName} references missing battle '{battle.BattleId}'.");
                    foreach (var (outcome, outcomeSteps) in battle.Outcomes)
                    {
                        ValidateStorySteps(outcomeSteps, repository, $"{ownerName} battle '{battle.BattleId}' outcome '{outcome}'");
                    }

                    break;
                case BranchStep branch:
                    Ensure(branch.Cases.Count > 0, $"{ownerName} has branch without cases.");
                    foreach (var branchCase in branch.Cases)
                    {
                        ValidateStorySteps(branchCase.Steps, repository, ownerName);
                    }

                    if (branch.Fallback is not null)
                    {
                        ValidateStorySteps(branch.Fallback, repository, $"{ownerName} branch fallback");
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unsupported story step type '{step.GetType().Name}'.");
            }
        }
    }

    private static void ValidateStoryMediaCommand(
        ParsedCall call,
        InMemoryContentRepository repository,
        string ownerName)
    {
        var (assetKind, argumentIndexes) = call.Root.Name switch
        {
            "background" => (MediaAssetKind.Texture, Enumerable.Range(0, Math.Min(1, call.Root.Arguments.Count))),
            // Legacy music calls may include a numeric transition/fade value
            // after the track id. Only the first argument is a resource id.
            "music" => (MediaAssetKind.Audio, Enumerable.Range(0, Math.Min(1, call.Root.Arguments.Count))),
            "sound" or "effect" => (MediaAssetKind.Audio, Enumerable.Range(0, Math.Min(1, call.Root.Arguments.Count))),
            "video" or "movie" => (MediaAssetKind.Video, Enumerable.Range(0, Math.Min(1, call.Root.Arguments.Count))),
            "set_portrait" or "head" when call.Root.Arguments.Count > 1 =>
                (MediaAssetKind.Texture, new[] { 1 }.AsEnumerable()),
            _ => (MediaAssetKind.Texture, Enumerable.Empty<int>()),
        };

        foreach (var argumentIndex in argumentIndexes)
        {
            if (!TryGetLiteralString(call.Root.Arguments[argumentIndex], out var reference))
            {
                continue;
            }

            ValidateRequiredMediaReference(
                reference,
                assetKind,
                $"{ownerName} command '{call.Root.Name}' argument {argumentIndex}",
                repository);
        }
    }

    private static bool TryGetLiteralString(ExpressionSyntax expression, out string value)
    {
        value = string.Empty;
        if (expression is not LiteralExpressionSyntax { Value.Kind: ExpressionValueKind.String } literal)
        {
            return false;
        }

        value = literal.Value.AsString("media reference");
        return true;
    }

    private static void ValidateChoiceOptions(
        IReadOnlyList<ChoiceOption> options,
        InMemoryContentRepository repository,
        string ownerName)
    {
        Ensure(options.Count > 0, $"{ownerName} has choice block without options.");
        foreach (var option in options)
        {
            ValidateStorySteps(option.Steps, repository, $"{ownerName} choice option '{option.Text}'");
        }
    }

    private static void ValidateMapStoryReferences(InMemoryContentRepository repository)
    {
        foreach (var trigger in repository.WorldTriggers)
        {
            ValidateActionReference(repository, trigger.Action, $"World trigger '{trigger.Id}'");
        }

        foreach (var map in repository.Maps.Values)
        {
            foreach (var location in map.Locations)
            {
                foreach (var mapEvent in location.Events)
                {
                    ValidateActionReference(repository, mapEvent.Action, $"Map '{map.Id}' location '{location.Id}'");
                }
            }
        }
    }

    private static void ValidateActionReference(
        InMemoryContentRepository repository,
        ParsedCall action,
        string owner)
    {
        ValidateMapCommandReference(repository, action, owner);
        if (IsMapCommand(action.Root.Name))
        {
            return;
        }

        if (!TryGetLiteralActionId(action, out var targetId))
        {
            return;
        }

        var exists = action.Root.Name switch
        {
            "story" => repository.StorySegments.ContainsKey(targetId),
            "shop" => repository.Shops.ContainsKey(targetId),
            "battle" => repository.Battles.ContainsKey(targetId),
            _ => true,
        };
        Ensure(exists, $"{owner} action '{action.Root.Name}' references missing target '{targetId}'.");
    }

    private static void ValidateMapCommandReference(
        InMemoryContentRepository repository,
        ParsedCall action,
        string owner)
    {
        if (!IsMapCommand(action.Root.Name) ||
            action.Root.Arguments.Count == 0)
        {
            return;
        }

        Ensure(action.Root.Arguments.Count <= 2,
            $"{owner} action '{action.Root.Name}' accepts at most a map id and location id.");
        if (!TryGetLiteralString(action.Root.Arguments[0], out var mapId))
        {
            return;
        }

        Ensure(repository.Maps.TryGetValue(mapId, out var map),
            $"{owner} action '{action.Root.Name}' references missing map '{mapId}'.");
        if (action.Root.Arguments.Count < 2 ||
            !TryGetLiteralString(action.Root.Arguments[1], out var locationId))
        {
            return;
        }

        Ensure(map!.Kind == MapKind.Large,
            $"{owner} action '{action.Root.Name}' cannot specify location '{locationId}' for small map '{mapId}'.");
        Ensure(!string.IsNullOrWhiteSpace(locationId),
            $"{owner} action '{action.Root.Name}' has an empty location id.");
        Ensure(map.Locations.Any(location => string.Equals(location.Id, locationId, StringComparison.Ordinal)),
            $"{owner} action '{action.Root.Name}' references missing location '{locationId}' in map '{mapId}'.");
    }

    private static bool IsMapCommand(string name) =>
        name is "map" or "set_map" or "tutorial";

    private static bool TryGetLiteralActionId(ParsedCall action, out string targetId)
    {
        targetId = string.Empty;
        if (action.Root.Arguments.Count != 1
            || action.Root.Arguments[0] is not LiteralExpressionSyntax
            {
                Value.Kind: ExpressionValueKind.String,
            } literal)
        {
            return false;
        }

        targetId = literal.Value.AsString("action target id");
        return true;
    }

}
