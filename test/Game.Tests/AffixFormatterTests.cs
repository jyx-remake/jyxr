using Game.Application.Formatters;
using Game.Content.Loading;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;

namespace Game.Tests;

public sealed class AffixFormatterTests
{
    private static string RealContentDirectoryPath =>
        Path.Combine(AppContext.BaseDirectory, "data");

    [Fact]
    public void AffixFormatter_FormatsSingleAffixesInChinese()
    {
        var talent = new TalentDefinition
        {
            Id = "talent_001",
            Name = "孤独求败",
        };
        var externalSkill = TestContentFactory.CreateExternalSkill("skill_001");
        externalSkill = externalSkill with { Name = "松风剑法" };
        var specialSkill = new SpecialSkillDefinition(
            "special_001",
            "凌波微步",
            "",
            "",
            0,
            SkillCostDefinition.None,
            null,
            "",
            "",
            []);
        var legendSkill = new LegendSkillDefinition(
            "legend_001",
            "无招胜有招",
            "skill_001",
            0.15d,
            [],
            []);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [externalSkill],
            specialSkills: [specialSkill],
            legendSkills: [legendSkill],
            talents: [talent]);

        Assert.Equal("攻击力 +12.5", AffixFormatter.FormatCn(
            new StatModifierAffix(StatType.Attack, ModifierValue.Add(12.5)),
            repository));
        Assert.Equal("命中率 +12.5%", AffixFormatter.FormatCn(
            new StatModifierAffix(StatType.Accuracy, ModifierValue.Add(0.125)),
            repository));
        Assert.Equal("集气速度 +0.125", AffixFormatter.FormatCn(
            new StatModifierAffix(StatType.Speed, ModifierValue.Add(0.125)),
            repository));
        Assert.Equal("集气速度提高5%", AffixFormatter.FormatCn(
            new StatModifierAffix(StatType.Speed, ModifierValue.Increase(0.05)),
            repository));
        Assert.Equal("天赋「孤独求败」", AffixFormatter.FormatCn(
            new GrantTalentAffix("talent_001"),
            repository));
        Assert.Equal("时装「独孤求败」", AffixFormatter.FormatCn(
            new GrantModelAffix("qiuchuji", 0, "独孤求败"),
            repository));
        Assert.Equal("技能「松风剑法」威力 +8%", AffixFormatter.FormatCn(
            new SkillBonusModifierAffix("skill_001", ModifierValue.Add(0.08)),
            repository));
        Assert.Equal("奇门类武功威力 +3%", AffixFormatter.FormatCn(
            new WeaponBonusModifierAffix(WeaponType.Qimen, ModifierValue.Add(0.03)),
            repository));
        Assert.Equal("绝技「无招胜有招」触发率提高12.5%", AffixFormatter.FormatCn(
            new LegendSkillChanceModifierAffix("legend_001", ModifierValue.Increase(0.125)),
            repository));
        Assert.Equal("技能「凌波微步」威力 +6%", AffixFormatter.FormatCn(
            new SkillBonusModifierAffix("special_001", ModifierValue.Add(0.06)),
            repository));
    }

    [Fact]
    public void AffixFormatter_FormatsSkillAffixesWithConditions()
    {
        var repository = TestContentFactory.CreateRepository(
            talents:
            [
                new TalentDefinition
                {
                    Id = "talent_001",
                    Name = "北冥神功",
                }
            ]);

        Assert.Equal(
            "天赋「北冥神功」",
            AffixFormatter.FormatCn(
                new SkillAffixDefinition(new GrantTalentAffix("talent_001")),
                repository));
        Assert.Equal(
            "10级解锁：攻击力 +8",
            AffixFormatter.FormatCn(
                new SkillAffixDefinition(new StatModifierAffix(StatType.Attack, ModifierValue.Add(8)), 10),
                repository));
        Assert.Equal(
            "10级解锁，装备生效：天赋「北冥神功」",
            AffixFormatter.FormatCn(
                new SkillAffixDefinition(new GrantTalentAffix("talent_001"), 10, true),
                repository));
    }

    [Fact]
    public void AffixFormatter_FormatsLinesInOrder()
    {
        var repository = TestContentFactory.CreateRepository(
            talents:
            [
                new TalentDefinition
                {
                    Id = "talent_001",
                    Name = "心眼通明",
                }
            ]);

        var lines = AffixFormatter.FormatLinesCn(
            [
                new StatModifierAffix(StatType.Attack, ModifierValue.Add(10)),
                new GrantTalentAffix("talent_001")
            ],
            repository);

        Assert.Equal(
            ["攻击力 +10", "天赋「心眼通明」"],
            lines);
    }

    [Fact]
    public void AffixFormatter_UsesReadableFallbackForUnresolvedSkillIds()
    {
        var repository = TestContentFactory.CreateRepository();

        var text = AffixFormatter.FormatCn(
            new SkillBonusModifierAffix("草头百姓的逆袭.这下子逆天了", ModifierValue.Add(0.15)),
            repository);

        Assert.Equal("技能「草头百姓的逆袭.这下子逆天了」威力 +15%", text);
    }

    [Fact]
    public void AffixFormatter_LoadedRepositoryFormatsExistingDataAffixes()
    {
        var repository = new JsonContentLoader().LoadFromDirectory(RealContentDirectoryPath);

        var equipment = repository.GetEquipment("独孤求败的草帽");
        var modelAffix = Assert.IsType<GrantModelAffix>(equipment.Affixes.Single(affix => affix is GrantModelAffix));
        Assert.Equal("时装「独孤求败」", AffixFormatter.FormatCn(modelAffix, repository));

        var internalSkill = repository.GetInternalSkill("北冥神功");
        Assert.Equal(
            "10级解锁，装备生效：天赋「北冥神功」",
            AffixFormatter.FormatCn(internalSkill.Affixes[0], repository));

        var externalSkill = repository.GetExternalSkill("野球拳");
        Assert.Equal(
            "10级解锁：拳掌类武功威力 +3%",
            AffixFormatter.FormatCn(externalSkill.Affixes[1], repository));
        Assert.Equal(
            "暴击率 +15%",
            AffixFormatter.FormatCn(equipment.Affixes[1], repository));
        Assert.Equal(
            "攻击力 +40",
            AffixFormatter.FormatCn(equipment.Affixes[0], repository));
        Assert.Equal(
            "15级解锁：绝技「草头百姓的逆袭.这下子逆天了」触发率 +5%",
            AffixFormatter.FormatCn(externalSkill.Affixes[3], repository));
    }

    [Fact]
    public void AffixFormatter_ThrowsForUnsupportedAffixes()
    {
        var repository = TestContentFactory.CreateRepository();

        Assert.Throws<NotSupportedException>(() => AffixFormatter.FormatCn(
            new HookAffix { Timing = HookTiming.OnBattleStart },
            repository));
        Assert.Throws<NotSupportedException>(() => AffixFormatter.FormatCn(
            new TraitAffix(TraitId.Swift),
            repository));
    }
}
