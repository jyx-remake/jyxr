using System.Text.Json;
using Game.Core.Affix;
using Game.Core.Battle;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Content.Loading;

public sealed partial class JsonContentLoader
{
    private const string GoldShopProductContentId = "元宝";
    private const string AchievementResourceGroup = "nick";
    private const string AchievementResourcePrefix = AchievementResourceGroup + ".";

    private static void ValidateRepository(InMemoryContentRepository repository)
    {
        ValidateCharacters(repository);
        ValidateBattles(repository);
        ValidateBattleHookAffixes(repository);
        ValidateSkillAffixes(repository);
        ValidateSpecialSkills(repository);
        ValidateItemReferences(repository);
        ValidateEquipmentRandomAffixTables(repository);
        ValidateShops(repository);
        ValidateLegendSkills(repository);
        ValidateWorldTriggers(repository);
        ValidateTowers(repository);
        ValidateStoryContent(repository);
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
        foreach (var table in repository.EquipmentRandomAffixTables)
        {
            Ensure(table.MinItemLevel > 0,
                "Equipment random affix table minItemLevel must be positive.");
            Ensure(table.MaxItemLevel >= table.MinItemLevel,
                $"Equipment random affix table level range '{table.MinItemLevel}-{table.MaxItemLevel}' is invalid.");
            Ensure(table.Options.Count > 0,
                $"Equipment random affix table level range '{table.MinItemLevel}-{table.MaxItemLevel}' has no options.");

            foreach (var option in table.Options)
            {
                Ensure(option.Weight > 0,
                    $"Equipment random affix option '{option.Kind}' must have positive weight.");

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

                if (option.Kind == EquipmentRandomAffixKind.WeaponBonus)
                {
                    Ensure(option.WeaponType is not null,
                        "Equipment random affix weapon bonus option must declare weaponType.");
                    Ensure(option.Ranges.Count > 0,
                        "Equipment random affix weapon bonus option must declare at least one range.");
                }
            }
        }
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
                    case StrengthenContextBuffBattleHookEffectDefinition:
                    case CancelHitBattleHookEffectDefinition:
                    case SetHitSuccessBattleHookEffectDefinition:
                        throw new InvalidOperationException(
                            $"SpecialSkill '{skill.Id}' uses unsupported hook-only effect '{effect.GetType().Name}'.");
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
        Ensure(position.X >= 0 && position.X < 11 && position.Y >= 0 && position.Y < 4,
            $"{ownerName} position ({position.X}, {position.Y}) exceeds battle grid size 11x4.");
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
                Ensure(!affix.RequiresEquippedInternalSkill,
                    $"ExternalSkill '{skill.Id}' skill affix cannot require equipped internal skill.");
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
        ValidateBattleHookAffix(affix.Effect!, ownerName, repository);
    }

    private static void ValidateBattleHookAffixes(InMemoryContentRepository repository)
    {
        foreach (var talent in repository.Talents.Values)
        {
            ValidateBattleHookAffixes(talent.Affixes, repository, $"Talent '{talent.Id}'");
        }

        foreach (var buff in repository.Buffs.Values)
        {
            ValidateBattleHookAffixes(buff.Affixes, repository, $"Buff '{buff.Id}'");
        }

        foreach (var equipment in repository.Equipments.Values)
        {
            ValidateBattleHookAffixes(equipment.Affixes, repository, $"Equipment '{equipment.Id}'");
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

        Ensure(hook.Effects.Count > 0 || hook.Speech is not null,
            $"{ownerName} has battle hook '{hook.Timing}' without effects or speech.");
        ValidateBattleHookSpeech(hook, ownerName);

        foreach (var condition in hook.Conditions)
        {
            switch (condition)
            {
                case ChanceBattleHookConditionDefinition chance:
                    Ensure(chance.Value >= 0d && chance.Value <= 1d,
                        $"{ownerName} has battle hook '{hook.Timing}' with invalid chance '{chance.Value}'.");
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
                case ContextSkillWeaponTypeBattleHookConditionDefinition skillWeaponTypeCondition:
                    Ensure(skillWeaponTypeCondition.WeaponTypes.Count > 0,
                        $"{ownerName} has battle hook '{hook.Timing}' skill weapon type condition without weaponTypes.");
                    foreach (var weaponType in skillWeaponTypeCondition.WeaponTypes)
                    {
                        Ensure(weaponType != WeaponType.Unknown,
                            $"{ownerName} has battle hook '{hook.Timing}' skill weapon type condition with unknown weapon type.");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"{ownerName} has unsupported battle hook condition '{condition.GetType().Name}'.");
            }
        }

        foreach (var effect in hook.Effects)
        {
            switch (effect)
            {
                case ModifyDamageBattleHookEffectDefinition:
                case ModifyMpCostBattleHookEffectDefinition:
                    break;
                case ModifyDamageContextBattleHookEffectDefinition modifyDamageContext:
                    ValidateModifyDamageContextEffect(modifyDamageContext, $"{ownerName} battle hook '{hook.Timing}'");
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
                case AddRageBattleEffectDefinition:
                case SetRageBattleEffectDefinition:
                case SetActionGaugeBattleEffectDefinition:
                case AddHpBattleEffectDefinition:
                case AddMpBattleEffectDefinition:
                case CancelHitBattleHookEffectDefinition:
                case SetHitSuccessBattleHookEffectDefinition:
                    ValidateSharedBattleEffect(effect, $"{ownerName} battle hook '{hook.Timing}'", repository);
                    break;
                default:
                    throw new InvalidOperationException($"{ownerName} has unsupported battle hook effect '{effect.GetType().Name}'.");
            }
        }
    }

    private static void ValidateModifyDamageContextEffect(
        ModifyDamageContextBattleHookEffectDefinition effect,
        string ownerName)
    {
        var hasRange = effect.DeltaMin is not null || effect.DeltaMax is not null;
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
                ValidateBattleTargetSelector(applyBuff.Target!, ownerName, null);
                break;
            case RemoveBuffBattleEffectDefinition removeBuff:
                Ensure(removeBuff.Target is not null, $"{ownerName} remove_buff effect is missing target.");
                Ensure(!string.IsNullOrWhiteSpace(removeBuff.BuffId), $"{ownerName} remove_buff effect is missing buffId.");
                if (repository is not null)
                {
                    Ensure(repository.Buffs.ContainsKey(removeBuff.BuffId), $"{ownerName} references missing buff '{removeBuff.BuffId}'.");
                }
                ValidateBattleTargetSelector(removeBuff.Target!, ownerName, null);
                break;
            case RemoveNegativeBuffsBattleEffectDefinition removeNegativeBuffs:
                Ensure(removeNegativeBuffs.Target is not null, $"{ownerName} remove_negative_buffs effect is missing target.");
                ValidateBattleTargetSelector(removeNegativeBuffs.Target!, ownerName, null);
                break;
            case RemovePositiveBuffsBattleEffectDefinition removePositiveBuffs:
                Ensure(removePositiveBuffs.Target is not null, $"{ownerName} remove_positive_buffs effect is missing target.");
                ValidateBattleTargetSelector(removePositiveBuffs.Target!, ownerName, null);
                break;
            case AddRageBattleEffectDefinition addRage:
                Ensure(addRage.Target is not null, $"{ownerName} add_rage effect is missing target.");
                Ensure(addRage.Value >= 0, $"{ownerName} add_rage effect has invalid value '{addRage.Value}'.");
                ValidateBattleTargetSelector(addRage.Target!, ownerName, null);
                break;
            case SetRageBattleEffectDefinition setRage:
                Ensure(setRage.Target is not null, $"{ownerName} set_rage effect is missing target.");
                Ensure(setRage.Value >= 0 && setRage.Value <= BattleUnit.MaxRage,
                    $"{ownerName} set_rage effect has invalid value '{setRage.Value}'.");
                ValidateBattleTargetSelector(setRage.Target!, ownerName, null);
                break;
            case SetActionGaugeBattleEffectDefinition setActionGauge:
                Ensure(setActionGauge.Target is not null, $"{ownerName} set_action_gauge effect is missing target.");
                Ensure(setActionGauge.Value >= 0, $"{ownerName} set_action_gauge effect has invalid value '{setActionGauge.Value}'.");
                ValidateBattleTargetSelector(setActionGauge.Target!, ownerName, null);
                break;
            case AddHpBattleEffectDefinition addHp:
                Ensure(addHp.Target is not null, $"{ownerName} add_hp effect is missing target.");
                Ensure(addHp.Value >= 0, $"{ownerName} add_hp effect has invalid value '{addHp.Value}'.");
                ValidateBattleTargetSelector(addHp.Target!, ownerName, null);
                break;
            case AddMpBattleEffectDefinition addMp:
                Ensure(addMp.Target is not null, $"{ownerName} add_mp effect is missing target.");
                Ensure(addMp.Value >= 0, $"{ownerName} add_mp effect has invalid value '{addMp.Value}'.");
                ValidateBattleTargetSelector(addMp.Target!, ownerName, null);
                break;
            case CancelHitBattleHookEffectDefinition:
            case SetHitSuccessBattleHookEffectDefinition:
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

    private static void ValidateBattleTargetSelector(
        BattleTargetSelectorDefinition selector,
        string ownerName,
        HookTiming? timing)
    {
        var scope = timing is null ? ownerName : $"{ownerName} '{timing}'";
        switch (selector)
        {
            case SelfBattleTargetSelectorDefinition:
            case SourceBattleTargetSelectorDefinition:
            case TargetBattleTargetSelectorDefinition:
            case AllAlliesBattleTargetSelectorDefinition:
            case AllEnemiesBattleTargetSelectorDefinition:
                break;
            case NearbyAlliesBattleTargetSelectorDefinition nearbyAllies:
                Ensure(nearbyAllies.Radius >= 0,
                    $"{scope} nearby_allies selector has invalid radius '{nearbyAllies.Radius}'.");
                break;
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

                    case GrantExternalSkillItemUseEffectDefinition externalSkill:
                        Ensure(!string.IsNullOrWhiteSpace(externalSkill.SkillId), $"Item '{item.Id}' external_skill effect is missing skillId.");
                        Ensure(externalSkillIds.Contains(externalSkill.SkillId), $"Item '{item.Id}' references missing external skill '{externalSkill.SkillId}'.");
                        break;

                    case GrantInternalSkillItemUseEffectDefinition internalSkill:
                        Ensure(!string.IsNullOrWhiteSpace(internalSkill.SkillId), $"Item '{item.Id}' internal_skill effect is missing skillId.");
                        Ensure(internalSkillIds.Contains(internalSkill.SkillId), $"Item '{item.Id}' references missing internal skill '{internalSkill.SkillId}'.");
                        break;

                    case GrantSpecialSkillItemUseEffectDefinition specialSkill:
                        Ensure(!string.IsNullOrWhiteSpace(specialSkill.SkillId), $"Item '{item.Id}' special_skill effect is missing skillId.");
                        Ensure(specialSkillIds.Contains(specialSkill.SkillId), $"Item '{item.Id}' references missing special skill '{specialSkill.SkillId}'.");
                        break;

                    case GrantTalentItemUseEffectDefinition talent:
                        Ensure(!string.IsNullOrWhiteSpace(talent.TalentId), $"Item '{item.Id}' grant_talent effect is missing talentId.");
                        Ensure(talentIds.Contains(talent.TalentId), $"Item '{item.Id}' references missing talent '{talent.TalentId}'.");
                        break;
                }
            }
        }
    }

    private static void ValidateShops(InMemoryContentRepository repository)
    {
        var itemIds = repository.Items.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var shop in repository.Shops.Values)
        {
            for (var index = 0; index < shop.Products.Count; index += 1)
            {
                var product = shop.Products[index];
                Ensure(!string.IsNullOrWhiteSpace(product.ContentId), $"Shop '{shop.Id}' product {index} is missing contentId.");
                Ensure(
                    itemIds.Contains(product.ContentId) ||
                    IsIgnoredShopProduct(product.ContentId),
                    $"Shop '{shop.Id}' product {index} references missing item '{product.ContentId}'.");
                Ensure(product.PurchaseLimit is null or >= 0, $"Shop '{shop.Id}' product {index} has invalid purchaseLimit.");
                Ensure(product.Price is null or >= 0, $"Shop '{shop.Id}' product {index} has invalid price.");
                Ensure(product.PremiumPrice is null or >= 0, $"Shop '{shop.Id}' product {index} has invalid premiumPrice.");
            }
        }
    }

    private static bool IsIgnoredShopProduct(string contentId) =>
        string.Equals(contentId, GoldShopProductContentId, StringComparison.Ordinal) ||
        contentId.EndsWith("残章", StringComparison.Ordinal);

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

                foreach (var reward in stage.Rewards)
                {
                    Ensure(!string.IsNullOrWhiteSpace(reward.ContentId),
                        $"Tower '{tower.Id}' stage '{stage.Id}' has reward with empty contentId.");
                    Ensure(repository.Items.ContainsKey(reward.ContentId) ||
                           string.Equals(reward.ContentId, GoldShopProductContentId, StringComparison.Ordinal),
                        $"Tower '{tower.Id}' stage '{stage.Id}' references missing reward item '{reward.ContentId}'.");
                    Ensure(reward.Probability >= 0d && reward.Probability <= 1d,
                        $"Tower '{tower.Id}' stage '{stage.Id}' reward '{reward.ContentId}' has invalid probability '{reward.Probability}'.");
                    Ensure(reward.MaxClaims is null or >= 0,
                        $"Tower '{tower.Id}' stage '{stage.Id}' reward '{reward.ContentId}' has invalid maxClaims '{reward.MaxClaims}'.");
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
                case CommandStep:
                    break;
                case JumpStep jump:
                    Ensure(repository.StorySegments.ContainsKey(jump.Target),
                        $"{ownerName} jumps to missing story segment '{jump.Target}'.");
                    break;
                case ChoiceStep choice:
                    Ensure(choice.Options.Count > 0, $"{ownerName} has choice without options.");
                    foreach (var option in choice.Options)
                    {
                        ValidateStorySteps(option.Steps, repository, $"{ownerName} choice option '{option.Text}'");
                    }

                    break;
                case BattleStep battle:
                    Ensure(repository.Battles.ContainsKey(battle.BattleId),
                        $"{ownerName} references missing battle '{battle.BattleId}'.");
                    Ensure(battle.Outcomes.Count > 0, $"{ownerName} battle '{battle.BattleId}' has no outcomes.");
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

    private static void ValidateMapStoryReferences(InMemoryContentRepository repository)
    {
        foreach (var trigger in repository.WorldTriggers)
        {
            ValidateWorldTriggerStoryReference(repository, trigger);
        }

        foreach (var map in repository.Maps.Values)
        {
            foreach (var location in map.Locations)
            {
                foreach (var mapEvent in location.Events)
                {
                    ValidateMapStoryReference(repository, map.Id, location.Id, mapEvent);
                }
            }
        }
    }

    private static void ValidateMapStoryReference(
        InMemoryContentRepository repository,
        string mapId,
        string locationId,
        MapEventDefinition mapEvent)
    {
        if (!string.Equals(mapEvent.Type, "story", StringComparison.Ordinal))
        {
            return;
        }

        Ensure(repository.StorySegments.ContainsKey(mapEvent.TargetId),
            $"Map '{mapId}' location '{locationId}' references missing story segment '{mapEvent.TargetId}'.");
    }

    private static void ValidateWorldTriggerStoryReference(
        InMemoryContentRepository repository,
        WorldTriggerDefinition trigger)
    {
        if (!string.Equals(trigger.Type, "story", StringComparison.Ordinal))
        {
            return;
        }

        Ensure(repository.StorySegments.ContainsKey(trigger.TargetId),
            $"World trigger '{trigger.Id}' references missing story segment '{trigger.TargetId}'.");
    }
}
