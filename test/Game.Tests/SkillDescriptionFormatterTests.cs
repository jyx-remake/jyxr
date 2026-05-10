using Game.Application.Formatters;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Model.Skills;

namespace Game.Tests;

public sealed class SkillDescriptionFormatterTests
{
    [Fact]
    public void SkillDescriptionFormatter_FormatsExternalSkillBbCode()
    {
        var buff = new BuffDefinition
        {
            Id = "bleed",
            Name = "流血",
            IsDebuff = true,
        };
        var talent = new TalentDefinition
        {
            Id = "battle_focus",
            Name = "战意高昂",
        };
        var skill = TestContentFactory.CreateExternalSkill(
            "songfeng",
            mpCost: 12,
            cooldown: 2,
            powerBase: 3,
            powerStep: 0.5,
            description: "入门剑法",
            type: WeaponType.Jianfa,
            affinity: 0.2,
            impactType: SkillImpactType.Line,
            impactSize: 4,
            castSize: 2,
            buffs: [new SkillBuffDefinition(buff, 2, 3, 75)],
            affixes:
            [
                new SkillAffixDefinition(new GrantTalentAffix("battle_focus"), 3),
                new SkillAffixDefinition(new StatModifierAffix(StatType.Attack, ModifierValue.Add(10f)), 5),
                new SkillAffixDefinition(new StatModifierAffix(StatType.Defence, ModifierValue.Add(12f)), 12),
            ]);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [skill],
            talents: [talent],
            buffs: [buff]);
        skill.Resolve(repository);

        var owner = CreateOwner();
        var instance = new ExternalSkillInstance(skill, owner, true)
        {
            Level = 4,
            Exp = 18,
            MaxLevel = 8,
            CurrentCooldown = 1,
        };

        var text = SkillDescriptionFormatter.FormatBbCodeCn(instance, repository);

        Assert.Contains("入门剑法", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]威力 4.5[/color]", text, StringComparison.Ordinal);
        Assert.Contains("覆盖类型 直线攻击", text, StringComparison.Ordinal);
        Assert.Contains("[color=cyan]消耗内力 12[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]技能CD 1/2[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]适性:阳20%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]特效：流血(2)[/color] [color=yellow]持续3回合[/color] [color=yellow]命中概率:75%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green](√)(3级解锁)天赋「战意高昂」[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red](×)(5级解锁)攻击力 +10[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red](×)(12级解锁)???[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SkillDescriptionFormatter_FormatsInternalSkillBbCode()
    {
        var talent = new TalentDefinition
        {
            Id = "beiming",
            Name = "北冥神功",
        };
        var skill = TestContentFactory.CreateInternalSkill(
            "beiming_internal",
            description: "逍遥派心法宝典",
            yin: 50,
            yang: 40,
            attackScale: 0.5,
            criticalScale: 0.2,
            defenceScale: 0.3,
            affixes:
            [
                new SkillAffixDefinition(new GrantTalentAffix("beiming"), 10, true)
            ]);
        var repository = TestContentFactory.CreateRepository(
            internalSkills: [skill],
            talents: [talent]);
        skill.Resolve(repository);

        var owner = CreateOwner();
        var instance = new InternalSkillInstance(skill, owner)
        {
            Level = 12,
            Exp = 90,
            MaxLevel = 15,
        };

        var text = SkillDescriptionFormatter.FormatBbCodeCn(instance, repository);

        Assert.Contains("逍遥派心法宝典", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]+攻击 60%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green]+防御 36%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]+爆发 20%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=cyan]阴适性 60[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]阳适性 48[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green](√)(10级解锁)装备生效：天赋「北冥神功」[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SkillDescriptionFormatter_FormatsLegendSkillBbCode()
    {
        var startSkill = TestContentFactory.CreateExternalSkill(
            "start_skill",
            mpCost: 10,
            rageCost: 4,
            powerBase: 5,
            powerStep: 0.2,
            impactType: SkillImpactType.Fan,
            impactSize: 3,
            castSize: 1);
        var requiredSkill = TestContentFactory.CreateExternalSkill("required_skill");
        var internalSkill = TestContentFactory.CreateInternalSkill("required_internal");
        var specialSkill = new SpecialSkillDefinition(
            "required_special",
            "凌波微步",
            "",
            "",
            0,
            SkillCostDefinition.None,
            null,
            "",
            "",
            null,
            []);
        var talent = new TalentDefinition
        {
            Id = "legend_talent",
            Name = "左右互搏",
        };
        var buff = new BuffDefinition
        {
            Id = "stun",
            Name = "晕眩",
            IsDebuff = true,
        };
        var legend = new LegendSkillDefinition(
            "legend_skill",
            "无招胜有招",
            "start_skill",
            0.15d,
            [
                new RequiredExternalSkillLevelLegendConditionDefinition("required_skill", 8),
                new RequiredInternalSkillLevelLegendConditionDefinition("required_internal", 6),
                new RequiredSpecialSkillLegendConditionDefinition("required_special"),
                new RequiredTalentLegendConditionDefinition("legend_talent"),
            ],
            [new SkillBuffDefinition(buff, 1, 2)],
            PowerExtra: 6d,
            RequiredLevel: 6);
        var repository = TestContentFactory.CreateRepository(
            externalSkills: [startSkill, requiredSkill],
            internalSkills: [internalSkill],
            specialSkills: [specialSkill],
            talents: [talent],
            legendSkills: [legend],
            buffs: [buff]);
        startSkill.Resolve(repository);
        requiredSkill.Resolve(repository);
        internalSkill.Resolve(repository);
        legend.Resolve(repository);

        var owner = CreateOwner();
        var parent = new ExternalSkillInstance(startSkill, owner, true)
        {
            Level = 4,
            Exp = 0,
            MaxLevel = 8,
        };
        var instance = new LegendSkillInstance(legend, parent);

        var text = SkillDescriptionFormatter.FormatBbCodeCn(instance, repository);

        Assert.Contains("[color=white]所属武学[/color] [color=red]start_skill[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=white]绝技解锁等级[/color] [color=red]6[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]触发概率 15%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]消耗怒气 4[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=black]需要外功「required_skill」达到8级[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=black]需要内功「required_internal」达到6级[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=black]需要特殊技能「凌波微步」[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=black]需要天赋「左右互搏」[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]特效：晕眩(1)[/color] [color=yellow]持续2回合[/color] [color=red]必定命中[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SkillDescriptionFormatter_FormatsFormSkillBbCode()
    {
        var form = new FormSkillDefinition(
            "songfeng_form",
            "风卷残云",
            "松风剑法变招。",
            "",
            5,
            1,
            new SkillCostDefinition(0, 2),
            new SkillTargetingDefinition(CastSize: 3, ImpactType: SkillImpactType.Star, ImpactSize: 2),
            1.5d,
            "",
            "",
            []);
        var external = TestContentFactory.CreateExternalSkill(
            "songfeng",
            mpCost: 12,
            powerBase: 3,
            powerStep: 0.5,
            impactType: SkillImpactType.Line,
            impactSize: 4,
            castSize: 2,
            formSkills: [form]);
        var repository = TestContentFactory.CreateRepository(externalSkills: [external]);
        external.Resolve(repository);

        var owner = CreateOwner();
        var parent = new ExternalSkillInstance(external, owner, true)
        {
            Level = 6,
            Exp = 18,
            MaxLevel = 10,
        };
        var instance = Assert.Single(parent.GetFormSkills());

        var text = SkillDescriptionFormatter.FormatBbCodeCn(instance, repository);

        Assert.Contains("[color=white]所属武学[/color] [color=red]songfeng[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=white]招式解锁等级[/color] [color=red]5[/color]", text, StringComparison.Ordinal);
        Assert.Contains("松风剑法变招。", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]威力 7[/color]", text, StringComparison.Ordinal);
        Assert.Contains("覆盖类型 米字攻击", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]消耗怒气 2[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green]技能CD 0/1[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SkillDescriptionFormatter_FormatsSpecialSkillBbCode()
    {
        var buff = new BuffDefinition
        {
            Id = "heal",
            Name = "疗伤",
            IsDebuff = false,
        };
        var skill = new SpecialSkillDefinition(
            "special_heal",
            "回春术",
            "快速恢复伤势。",
            "heal_icon",
            3,
            new SkillCostDefinition(20, 2),
            new SkillTargetingDefinition(CastSize: 1, ImpactType: SkillImpactType.Single, ImpactSize: 2),
            "heal_anim",
            "heal_audio",
            null,
            [new SkillBuffDefinition(buff, 3, 2, 100)]);
        var repository = TestContentFactory.CreateRepository(
            specialSkills: [skill],
            buffs: [buff]);
        skill.Resolve(repository);

        var instance = new SpecialSkillInstance(skill, CreateOwner(), true)
        {
            CurrentCooldown = 0,
        };

        var text = SkillDescriptionFormatter.FormatBbCodeCn(instance, repository);

        Assert.Contains("快速恢复伤势。", text, StringComparison.Ordinal);
        Assert.Contains("覆盖类型 点攻击", text, StringComparison.Ordinal);
        Assert.Contains("[color=cyan]消耗内力 20[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]消耗怒气 2[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green]技能CD 0/3[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]特效：疗伤(3)[/color] [color=yellow]持续2回合[/color] [color=red]必定命中[/color]", text, StringComparison.Ordinal);
    }

    private static CharacterInstance CreateOwner()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        return TestContentFactory.CreateCharacterInstance("hero_001", definition);
    }
}
