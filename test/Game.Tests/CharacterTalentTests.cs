using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class CharacterTalentTests
{
    [Fact]
    public void CreateInitial_RebuildsSnapshotForEquippedInternalSkillGrantedTalent()
    {
        var radiance = new TalentDefinition
        {
            Id = "radiance",
            Name = "Radiance",
        };
        var grantRadiance = new GrantTalentAffix("radiance");
        grantRadiance.Resolve(TestContentFactory.CreateRepository(talents: [radiance]));
        var internalSkill = TestContentFactory.CreateInternalSkill(
            "inner_light",
            affixes:
            [
                new SkillAffixDefinition(
                    grantRadiance,
                    MinimumLevel: 10,
                    RequiresEquippedInternalSkill: true),
            ]);
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_monk",
            internalSkills:
            [
                new InitialInternalSkillEntryDefinition(
                    internalSkill,
                    Level: 10,
                    Equipped: true),
            ],
            talents: []);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);

        Assert.True(character.HasEffectiveTalent("radiance"));
        Assert.Contains(character.EffectiveTalents, talent => talent.Id == "radiance");
    }

    [Fact]
    public void CreateInitial_RebuildsSnapshotForLearnedTalentAffixes()
    {
        var battleFocus = new TalentDefinition
        {
            Id = "battle_focus",
            Name = "Battle Focus",
            Affixes =
            [
                new StatModifierAffix(StatType.Fuyuan, ModifierValue.Add(7)),
            ],
        };
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            new Dictionary<StatType, int>
            {
                [StatType.Fuyuan] = 10,
            },
            talents: [battleFocus]);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);

        Assert.True(character.HasEffectiveTalent("battle_focus"));
        Assert.Equal(17, character.GetStat(StatType.Fuyuan));
    }

    [Fact]
    public void RebuildSnapshot_UpperTalentReplacesLearnedLowerTalent()
    {
        var lower = new TalentDefinition
        {
            Id = "qiankun_shift",
            Name = "乾坤大挪移",
            Affixes =
            [
                new StatModifierAffix(StatType.Fuyuan, ModifierValue.Add(7)),
            ],
        };
        var upper = new TalentDefinition
        {
            Id = "qiankun_shift_aoyi",
            Name = "乾坤大挪移奥义",
            ReplaceTalentIds = ["qiankun_shift"],
            Affixes =
            [
                new StatModifierAffix(StatType.Dingli, ModifierValue.Add(4)),
            ],
        };
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            new Dictionary<StatType, int>
            {
                [StatType.Fuyuan] = 10,
                [StatType.Dingli] = 10,
            },
            talents: [lower, upper]);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);

        Assert.True(character.HasEffectiveTalent("qiankun_shift"));
        Assert.True(character.HasEffectiveTalent("qiankun_shift_aoyi"));
        Assert.Equal(10, character.GetStat(StatType.Fuyuan));
        Assert.Equal(14, character.GetStat(StatType.Dingli));
    }

    [Fact]
    public void RebuildSnapshot_UpperTalentBlocksGrantedLowerTalent()
    {
        var lower = new TalentDefinition
        {
            Id = "qiankun_shift",
            Name = "乾坤大挪移",
            Affixes =
            [
                new StatModifierAffix(StatType.Fuyuan, ModifierValue.Add(7)),
            ],
        };
        var upper = new TalentDefinition
        {
            Id = "qiankun_shift_aoyi",
            Name = "乾坤大挪移奥义",
            ReplaceTalentIds = ["qiankun_shift"],
            Affixes =
            [
                new StatModifierAffix(StatType.Dingli, ModifierValue.Add(4)),
            ],
        };
        var repository = TestContentFactory.CreateRepository(talents: [lower, upper]);
        var grantLower = new GrantTalentAffix("qiankun_shift");
        grantLower.Resolve(repository);
        var skill = TestContentFactory.CreateExternalSkill(
            "grant_lower",
            affixes:
            [
                new SkillAffixDefinition(grantLower),
            ]);
        var definition = TestContentFactory.CreateCharacterDefinition(
            "hero_knight",
            new Dictionary<StatType, int>
            {
                [StatType.Fuyuan] = 10,
                [StatType.Dingli] = 10,
            },
            externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 1)],
            talents: [upper]);

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);

        Assert.True(character.HasEffectiveTalent("qiankun_shift"));
        Assert.True(character.HasEffectiveTalent("qiankun_shift_aoyi"));
        Assert.Equal(10, character.GetStat(StatType.Fuyuan));
        Assert.Equal(14, character.GetStat(StatType.Dingli));
    }

    [Fact]
    public void RebuildSnapshot_DoesNotExpandGrantTalentAffixBlockedBySkillCondition()
    {
        var battleFocus = new TalentDefinition
        {
            Id = "battle_focus",
            Name = "Battle Focus",
            Affixes =
            [
                new StatModifierAffix(StatType.Fuyuan, ModifierValue.Add(7)),
            ],
        };
        var grantAffix = new GrantTalentAffix("battle_focus");
        grantAffix.Resolve(TestContentFactory.CreateRepository(talents: [battleFocus]));
        var skill = TestContentFactory.CreateExternalSkill(
            "late_focus",
            affixes:
            [
                new SkillAffixDefinition(
                    grantAffix,
                    MinimumLevel: 5),
            ]);
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001",
            TestContentFactory.CreateCharacterDefinition(
                "hero_knight",
                new Dictionary<StatType, int>
                {
                    [StatType.Fuyuan] = 10,
                },
                externalSkills: [new InitialExternalSkillEntryDefinition(skill, Level: 1)]));

        character.RebuildSnapshot();

        Assert.Empty(character.EffectiveTalents);
        Assert.False(character.HasEffectiveTalent("battle_focus"));
        Assert.Equal(10, character.GetStat(StatType.Fuyuan));
    }
}
