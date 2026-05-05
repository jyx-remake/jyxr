using Game.Application.Formatters;
using Game.Content.Loading;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Tests;

public sealed class ItemDescriptionFormatterTests
{
    private static string RealContentDirectoryPath =>
        Path.Combine(AppContext.BaseDirectory, "data");

    [Fact]
    public void ItemDescriptionFormatter_FormatsConsumableItemBbCode()
    {
        var buff = new BuffDefinition
        {
            Id = "drunk",
            Name = "醉酒",
            IsDebuff = false,
        };
        var item = new NormalItemDefinition
        {
            Id = "wine",
            Name = "烧刀子",
            Type = ItemType.Consumable,
            Description = "烈酒入喉，胆气横生。",
            Cooldown = 8,
            UseEffects =
            [
                new AddRageItemUseEffectDefinition(2),
                new AddBuffItemUseEffectDefinition("drunk", Level: 0, Duration: 3),
            ]
        };
        var repository = TestContentFactory.CreateRepository(
            items: [item],
            buffs: [buff]);

        var text = ItemDescriptionFormatter.FormatBbCodeCn(item, repository);

        Assert.Contains("[color=white]烈酒入喉，胆气横生。[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]使用效果：[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]怒气 +2[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]附加状态「醉酒」（等级 0，持续 3 回合）[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=black]冷却 8 回合[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemDescriptionFormatter_FormatsSkillAndTalentBookEffectsWithResolvedNames()
    {
        var externalSkill = TestContentFactory.CreateExternalSkill("songfeng") with
        {
            Name = "松风剑法",
        };
        var talent = new TalentDefinition
        {
            Id = "battle_focus",
            Name = "战意高昂",
        };
        var item = new NormalItemDefinition
        {
            Id = "manual_songfeng",
            Name = "松风剑谱",
            Type = ItemType.SkillBook,
            Description = "青城派入门剑谱。",
            Requirements =
            [
                new StatItemRequirementDefinition(StatType.Jianfa, 30),
                new TalentItemRequirementDefinition("battle_focus"),
            ],
            UseEffects =
            [
                new GrantExternalSkillItemUseEffectDefinition("songfeng", 10),
                new GrantTalentItemUseEffectDefinition("battle_focus"),
            ]
        };
        var repository = TestContentFactory.CreateRepository(
            items: [item],
            externalSkills: [externalSkill],
            talents: [talent]);

        var text = ItemDescriptionFormatter.FormatBbCodeCn(item, repository);

        Assert.Contains("[color=red]使用要求：[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]剑法 >= 30[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]需要天赋「战意高昂」[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]学会外功「松风剑法」（10级）[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]获得天赋「战意高昂」[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemDescriptionFormatter_FormatsEquipmentDefinitionBbCode()
    {
        var talent = new TalentDefinition
        {
            Id = "sword_mastery",
            Name = "剑系装备",
        };
        var equipment = TestContentFactory.CreateEquipment("iron_sword") with
        {
            Name = "精钢长剑",
            Description = "寻常剑客常用的精钢长剑。",
            Requirements =
            [
                new StatItemRequirementDefinition(StatType.Jianfa, 25),
                new StatItemRequirementDefinition(StatType.Shenfa, 20),
            ],
            Affixes =
            [
                new StatModifierAffix(StatType.Attack, ModifierValue.Add(40)),
                new StatModifierAffix(StatType.CritChance, ModifierValue.Add(0.02)),
                new GrantTalentAffix("sword_mastery"),
            ]
        };
        var repository = TestContentFactory.CreateRepository(
            equipment: [equipment],
            talents: [talent]);

        var text = ItemDescriptionFormatter.FormatBbCodeCn(equipment, repository);

        Assert.Contains("[color=red]装备要求：[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]剑法 >= 25[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=red]身法 >= 20[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]装备词条：[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]攻击力 +40[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]暴击率 +2%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]天赋「剑系装备」[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemDescriptionFormatter_FormatsEquipmentInstanceExtraAffixesSeparately()
    {
        var equipment = TestContentFactory.CreateEquipment("ward_charm", EquipmentSlotType.Accessory) with
        {
            Name = "护身玉佩",
            Description = "温润玉佩，佩之宁神。",
            Affixes =
            [
                new StatModifierAffix(StatType.Defence, ModifierValue.Add(18)),
            ]
        };
        var repository = TestContentFactory.CreateRepository(equipment: [equipment]);
        var instance = new EquipmentInstance(
            "ward_charm_00000001",
            equipment,
            [
                new StatModifierAffix(StatType.CritChance, ModifierValue.Add(0.05)),
                new GrantTalentAffix("ghost_step"),
            ]);

        var text = ItemDescriptionFormatter.FormatBbCodeCn(instance, repository);

        Assert.Contains("[color=yellow]装备词条：[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]防御力 +18[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green]附加词条：[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green]暴击率 +5%[/color]", text, StringComparison.Ordinal);
        Assert.Contains("[color=green]天赋「ghost_step」[/color]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemDescriptionFormatter_LoadedRepositoryFormatsExistingDataItems()
    {
        var repository = new JsonContentLoader().LoadFromDirectory(RealContentDirectoryPath);

        var herb = repository.GetItem("止血草");
        var herbText = ItemDescriptionFormatter.FormatBbCodeCn(herb, repository);
        Assert.Contains("[color=white]常见的草药，有止血之功效[/color]", herbText, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]恢复气血 360[/color]", herbText, StringComparison.Ordinal);

        var woodenBlade = repository.GetEquipment("木刀");
        var equipmentText = ItemDescriptionFormatter.FormatBbCodeCn(woodenBlade, repository);
        Assert.Contains("[color=yellow]装备词条：[/color]", equipmentText, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]攻击力 +8[/color]", equipmentText, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]暴击率 +1%[/color]", equipmentText, StringComparison.Ordinal);
        Assert.Contains("[color=yellow]天赋「刀系装备」[/color]", equipmentText, StringComparison.Ordinal);
    }
}
