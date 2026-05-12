using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Tests;

public sealed class CharacterTests
{
    [Fact]
    public void CreateInitial_BuildsPersistentCharacterFromDefinition()
    {
        var basicAttack = TestContentFactory.CreateExternalSkill("basic_attack");
        var battleFocus = CreateTalent("battle_focus");
        var ironBlade = TestContentFactory.CreateEquipment("iron_blade");
        var bloodRush = new SpecialSkillDefinition(
            "blood_rush",
            "blood_rush",
            "",
            "",
            0,
            new SkillCostDefinition(0, 0),
            new SkillTargetingDefinition(),
            "",
            "",
            null,
            []);
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            new Dictionary<StatType, int>
            {
                [StatType.MaxHp] = 40,
                [StatType.Gengu] = 8,
                [StatType.Bili] = 20,
            },
            [new InitialExternalSkillEntryDefinition(basicAttack, MaxLevel: 8)],
            talents: [battleFocus],
            equipment: [ironBlade],
            level: 3,
            portrait: "portrait.hero_knight",
            model: "knight_model",
            gender: CharacterGender.Male,
            growTemplate: "knight",
            arenaEnabled: true,
            specialSkills: [bloodRush]);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);

        Assert.Equal("char_001", character.Id);
        Assert.Equal("hero_knight", character.Definition.Id);
        Assert.Equal("hero_knight", character.Name);
        Assert.Equal(3, character.Level);
        Assert.Equal(CharacterLevelProgression.GetTotalExperienceRequiredForLevel(3), character.Experience);
        Assert.Single(character.ExternalSkills);
        Assert.Equal("basic_attack", character.ExternalSkills[0].Definition.Id);
        Assert.Equal(1, character.ExternalSkills[0].Level);
        Assert.Equal(20, character.ExternalSkills[0].MaxLevel);
        Assert.True(character.ExternalSkills[0].IsActive);
        Assert.Same(character, character.ExternalSkills[0].Owner);
        Assert.Equal(["battle_focus"], character.UnlockedTalents.Select(talent => talent.Id).ToArray());
        Assert.Equal(["blood_rush"], character.SpecialSkills.Select(skill => skill.Id).ToArray());
        Assert.Equal(CharacterGender.Male, character.Definition.Gender);
        Assert.Equal("portrait.hero_knight", character.Definition.Portrait);
        Assert.Equal("knight_model", character.Definition.Model);
        Assert.Equal("portrait.hero_knight", character.Portrait);
        Assert.Equal("knight_model", character.Model);
        Assert.Equal("knight", character.Definition.GrowTemplate);
        Assert.Equal("knight", character.GrowTemplateId);
        Assert.True(character.Definition.ArenaEnabled);
        Assert.Equal(20, character.Definition.Stats[StatType.Bili]);
        Assert.Single(character.EquippedItems);
        Assert.Equal("iron_blade", character.EquippedItems[EquipmentSlotType.Weapon].Definition.Id);
    }

    [Fact]
    public void SetGrowTemplate_UpdatesInstanceStateWithoutChangingDefinition()
    {
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            growTemplate: "knight");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        character.GrowTemplateId = "wanderer";

        Assert.Equal("wanderer", character.GrowTemplateId);
        Assert.Equal("knight", character.Definition.GrowTemplate);
    }

    [Fact]
    public void GetResolvedStat_UsesBaseStatsForOutOfBattleValue()
    {
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            new Dictionary<StatType, int>
            {
                [StatType.Bili] = 10,
                [StatType.Gengu] = 8,
            });

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        character.GrantStatPoints(3);
        character.AllocateStat(StatType.Bili, 3);

        var calculator = new CharacterStatCalculator();
        var sheet = calculator.Calculate(character);

        Assert.Equal(13, sheet.BaseStats[StatType.Bili]);
        Assert.Equal(13, sheet.FinalStats[StatType.Bili]);
        Assert.Equal(8, sheet.FinalStats[StatType.Gengu]);
        Assert.Equal(0, character.UnspentStatPoints);
        Assert.Equal(13, character.BaseStats[StatType.Bili]);
    }

    [Fact]
    public void RebuildSnapshot_AppliesExternalSkillAffixesAfterMinimumLevel()
    {
        var affix = new SkillAffixDefinition(
            new StatModifierAffix(StatType.Bili, ModifierValue.Add(5)),
            MinimumLevel: 3);
        var skill = TestContentFactory.CreateExternalSkill(
            "focus_strike",
            affixes: [affix]);

        var lockedCharacter = TestContentFactory.CreateCharacterInstance(
            "char_locked",
            TestContentFactory.CreateCharacterDefinition(
                "hero_locked",
                new Dictionary<StatType, int>
                {
                    [StatType.Bili] = 10,
                },
                externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 2)]));
        var availableCharacter = TestContentFactory.CreateCharacterInstance(
            "char_available",
            TestContentFactory.CreateCharacterDefinition(
                "hero_available",
                new Dictionary<StatType, int>
                {
                    [StatType.Bili] = 10,
                },
                externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 3)]));

        lockedCharacter.RebuildSnapshot();
        availableCharacter.RebuildSnapshot();

        Assert.Equal(10, lockedCharacter.GetStat(StatType.Bili));
        Assert.Equal(15, availableCharacter.GetStat(StatType.Bili));
    }

    [Fact]
    public void SetExternalSkillState_RebuildsSnapshotForRuntimeSkillAffixes()
    {
        var affix = new SkillAffixDefinition(
            new StatModifierAffix(StatType.Bili, ModifierValue.Add(5)),
            MinimumLevel: 3);
        var skill = TestContentFactory.CreateExternalSkill(
            "focus_strike",
            affixes: [affix]);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                new Dictionary<StatType, int>
                {
                    [StatType.Bili] = 10,
                }));

        character.SetExternalSkillState(skill, 3, 0, true);

        Assert.Equal(15, character.GetStat(StatType.Bili));
    }

    [Fact]
    public void SetExternalSkillActive_UpdatesExistingInstanceWithoutReplacingIt()
    {
        var skill = TestContentFactory.CreateExternalSkill("focus_strike");
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 3)]));

        var instance = character.ExternalSkills[0];
        var changed = character.SetExternalSkillActive(skill.Id, false);

        Assert.True(changed);
        Assert.Same(instance, character.ExternalSkills[0]);
        Assert.False(instance.IsActive);
    }

    [Fact]
    public void SetSpecialSkillActive_UpdatesExistingInstanceWithoutReplacingIt()
    {
        var skill = new SpecialSkillDefinition(
            "blood_rush",
            "blood_rush",
            "",
            "",
            0,
            SkillCostDefinition.None,
            null,
            "",
            "",
            null,
            []);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                specialSkills: [skill]));

        var instance = character.SpecialSkills[0];
        var changed = character.SetSpecialSkillActive(skill.Id, false);

        Assert.True(changed);
        Assert.Same(instance, character.SpecialSkills[0]);
        Assert.False(instance.IsActive);
    }

    [Fact]
    public void RebuildSnapshot_AppliesInternalSkillAffixWithoutEquipRequirement()
    {
        var affix = new SkillAffixDefinition(
            new StatModifierAffix(StatType.Dingli, ModifierValue.Add(4)));
        var skill = TestContentFactory.CreateInternalSkill(
            "breath_control",
            affixes: [affix]);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                new Dictionary<StatType, int>
                {
                    [StatType.Dingli] = 8,
                },
                internalSkills: [new InitialInternalSkillEntryDefinition(skill)]));

        character.RebuildSnapshot();

        Assert.False(character.InternalSkills[0].IsEquipped);
        Assert.Equal(12, character.GetStat(StatType.Dingli));
    }

    [Fact]
    public void RebuildSnapshot_RequiresEquippedInternalSkillWhenSkillAffixRequestsIt()
    {
        var affix = new SkillAffixDefinition(
            new StatModifierAffix(StatType.Gengu, ModifierValue.Add(6)),
            RequiresEquippedInternalSkill: true);
        var skill = TestContentFactory.CreateInternalSkill(
            "iron_body",
            affixes: [affix]);

        var unequippedCharacter = TestContentFactory.CreateCharacterInstance(
            "char_unequipped",
            TestContentFactory.CreateCharacterDefinition(
                "hero_unequipped",
                new Dictionary<StatType, int>
                {
                    [StatType.Gengu] = 9,
                },
                internalSkills: [new InitialInternalSkillEntryDefinition(skill)]));
        var equippedCharacter = TestContentFactory.CreateCharacterInstance(
            "char_equipped",
            TestContentFactory.CreateCharacterDefinition(
                "hero_equipped",
                new Dictionary<StatType, int>
                {
                    [StatType.Gengu] = 9,
                },
                internalSkills: [new InitialInternalSkillEntryDefinition(skill, Equipped: true)]));

        unequippedCharacter.RebuildSnapshot();
        equippedCharacter.RebuildSnapshot();

        Assert.Equal(9, unequippedCharacter.GetStat(StatType.Gengu));
        Assert.Equal(15, equippedCharacter.GetStat(StatType.Gengu));
    }

    [Fact]
    public void EquipInternalSkill_SwitchesSingleEquippedSkill()
    {
        var flame = TestContentFactory.CreateInternalSkill("flame");
        var frost = TestContentFactory.CreateInternalSkill("frost");
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                internalSkills:
                [
                    new InitialInternalSkillEntryDefinition(flame, Equipped: true),
                    new InitialInternalSkillEntryDefinition(frost),
                ]));

        var changed = character.EquipInternalSkill(frost.Id);

        Assert.True(changed);
        Assert.Equal(frost.Id, character.EquippedInternalSkillId);
        Assert.False(character.InternalSkills[0].IsEquipped);
        Assert.True(character.InternalSkills[1].IsEquipped);
    }

    [Fact]
    public void InternalSkillInstance_ResolvesLegacyCoreValuesFromLevel()
    {
        var definition = TestContentFactory.CreateInternalSkill(
            "basic_internal",
            description: "天下内功的根基",
            icon: "icon_neigong_001",
            yin: 25,
            yang: 25,
            attackScale: 0.15,
            criticalScale: 0.15,
            defenceScale: 0.15,
            hard: 1d);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                internalSkills: [new InitialInternalSkillEntryDefinition(definition, Level: 5)]));
        var skill = character.InternalSkills[0];

        Assert.Equal(12, skill.Yin);
        Assert.Equal(12, skill.Yang);
        Assert.Equal(0.075d, skill.AttackRatio, 6);
        Assert.Equal(0.075d, skill.DefenceRatio, 6);
        Assert.Equal(0.075d, skill.CriticalRatio, 6);
        Assert.Equal(112, skill.LevelUpExp);
    }

    [Fact]
    public void ExternalSkillInstance_ResolvesLegacyLevelUpExp()
    {
        var definition = TestContentFactory.CreateExternalSkill(
            "basic_external",
            hard: 3d);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                externalSkills: [new InitialExternalSkillEntryDefinition(definition, Level: 8)]));
        var skill = character.ExternalSkills[0];

        Assert.Equal(240, skill.LevelUpExp);
    }

    [Fact]
    public void ExternalSkillInstance_GetFormSkills_ReflectsUnlockAndEnabledState()
    {
        var form = new FormSkillDefinition(
            "sword_form",
            "Sword Form",
            "",
            "",
            3,
            2,
            new SkillCostDefinition(5, 2),
            new SkillTargetingDefinition(CastSize: 0, ImpactSize: 0),
            4d,
            "",
            "",
            []);
        var definition = TestContentFactory.CreateExternalSkill(
            "sword_skill",
            formSkills: [form],
            powerBase: 3d,
            powerStep: 2d);
        var lockedCharacter = TestContentFactory.CreateCharacterInstance(
            "char_locked",
            TestContentFactory.CreateCharacterDefinition(
                "hero_locked",
                externalSkills: [new InitialExternalSkillEntryDefinition(definition, Level: 2)]));
        var locked = lockedCharacter.ExternalSkills[0].GetFormSkills().Single();
        Assert.Equal(FormSkillInstanceState.Locked, locked.State);
        Assert.False(locked.IsActive);
        Assert.Equal(9d, locked.Power, 6);

        var disabledCharacter = TestContentFactory.CreateCharacterInstance(
            "char_disabled",
            TestContentFactory.CreateCharacterDefinition(
                "hero_disabled",
                externalSkills: [new InitialExternalSkillEntryDefinition(definition, Level: 3)]));
        disabledCharacter.SetExternalSkillState(definition, 3, 0, false);
        var disabled = disabledCharacter.ExternalSkills[0].GetFormSkills().Single();
        Assert.Equal(FormSkillInstanceState.Disabled, disabled.State);
        Assert.False(disabled.IsActive);
        Assert.Equal(11d, disabled.Power, 6);
        Assert.Equal(2, disabled.Cooldown);
        Assert.Equal(5, disabled.MpCost);
        Assert.Equal(2, disabled.RageCost);

        var availableCharacter = TestContentFactory.CreateCharacterInstance(
            "char_available",
            TestContentFactory.CreateCharacterDefinition(
                "hero_available",
                externalSkills: [new InitialExternalSkillEntryDefinition(definition, Level: 3)]));
        var available = availableCharacter.ExternalSkills[0].GetFormSkills().Single();
        Assert.Equal(FormSkillInstanceState.Available, available.State);
        Assert.True(available.IsActive);
    }

    [Fact]
    public void InternalSkillInstance_GetFormSkills_ReflectsEquipState()
    {
        var form = new FormSkillDefinition(
            "poison_form",
            "Poison Form",
            "",
            "",
            5,
            3,
            new SkillCostDefinition(4, 1),
            new SkillTargetingDefinition(CastSize: 0, ImpactSize: 0),
            3d,
            "",
            "",
            []);
        var definition = TestContentFactory.CreateInternalSkill(
            "poison_internal",
            attackScale: 0.2d,
            formSkills: [form]);
        var unequippedCharacter = TestContentFactory.CreateCharacterInstance(
            "char_unequipped",
            TestContentFactory.CreateCharacterDefinition(
                "hero_unequipped",
                internalSkills: [new InitialInternalSkillEntryDefinition(definition, Level: 5)]));
        var equippedCharacter = TestContentFactory.CreateCharacterInstance(
            "char_equipped",
            TestContentFactory.CreateCharacterDefinition(
                "hero_equipped",
                internalSkills: [new InitialInternalSkillEntryDefinition(definition, Level: 5, Equipped: true)]));

        var unequipped = unequippedCharacter.InternalSkills[0].GetFormSkills().Single();
        Assert.False(unequippedCharacter.InternalSkills[0].IsActive);
        Assert.Equal(FormSkillInstanceState.SourceNotEquipped, unequipped.State);
        Assert.False(unequipped.IsActive);
        Assert.Equal(4.3d, unequipped.Power, 6);
        Assert.Same(unequippedCharacter.InternalSkills[0], unequipped.Parent);
        Assert.Same(unequippedCharacter, unequipped.Owner);

        var equipped = equippedCharacter.InternalSkills[0].GetFormSkills().Single();
        Assert.True(equippedCharacter.InternalSkills[0].IsActive);
        Assert.Equal(FormSkillInstanceState.Available, equipped.State);
        Assert.True(equipped.IsActive);
        Assert.Equal(3, equipped.Cooldown);
        Assert.Equal(4, equipped.MpCost);
        Assert.Equal(1, equipped.RageCost);
        Assert.Same(equippedCharacter.InternalSkills[0], equipped.Parent);
        Assert.Same(equippedCharacter, equipped.Owner);
    }

    [Fact]
    public void Character_GetFormSkills_AggregatesExternalAndInternalForms()
    {
        var externalForm = new FormSkillDefinition(
            "sword_form",
            "Sword Form",
            "",
            "",
            1,
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(CastSize: 0, ImpactSize: 0),
            4d,
            "",
            "",
            []);
        var internalForm = new FormSkillDefinition(
            "poison_form",
            "Poison Form",
            "",
            "",
            1,
            0,
            new SkillCostDefinition(),
            new SkillTargetingDefinition(CastSize: 0, ImpactSize: 0),
            3d,
            "",
            "",
            []);
        var external = TestContentFactory.CreateExternalSkill(
            "sword_skill",
            formSkills: [externalForm],
            powerBase: 3d,
            powerStep: 2d);
        var internalSkill = TestContentFactory.CreateInternalSkill(
            "poison_internal",
            attackScale: 0.2d,
            formSkills: [internalForm]);
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            externalSkills: [new InitialExternalSkillEntryDefinition(external, Level: 3)],
            internalSkills: [new InitialInternalSkillEntryDefinition(internalSkill, Level: 5, Equipped: true)]);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var forms = character.GetFormSkills();

        Assert.Equal(2, forms.Count);
        Assert.Contains(forms, form => form.Definition.Id == "sword_form" && form.State == FormSkillInstanceState.Available);
        Assert.Contains(forms, form => form.Definition.Id == "poison_form" && form.State == FormSkillInstanceState.Available);
    }

    private static TalentDefinition CreateTalent(
        string id) =>
        new()
        {
            Id = id,
            Name = id,
        };

    [Fact]
    public void AddEquipmentInstance_RejectsDuplicateSlotType()
    {
        var ironBlade = TestContentFactory.CreateEquipment("iron_blade", EquipmentSlotType.Weapon);
        var steelSword = TestContentFactory.CreateEquipment("steel_sword", EquipmentSlotType.Weapon);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                equipment: [ironBlade]));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            character.AddEquipmentInstance(new EquipmentInstance("equip_weapon_002", steelSword)));

        Assert.Contains("Weapon", exception.Message, StringComparison.Ordinal);
    }
}
