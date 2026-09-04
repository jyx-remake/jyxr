using Game.Application.Formatters;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Tests;

public sealed class TitleDescriptionFormatterTests
{
    private static CharacterTitleDefinition MojiaJuzi() => new()
    {
        Id = "墨家巨子",
        Name = "墨家巨子",
        Description = "墨家有着严密的组织",
        Attack = 10.0,
        Defence = 10.0,
        Hard = 5.0,
        AoyiProbabilityAdd = 0.1,
        AoyiPowerAdd = 10.0,
        Affixes =
        [
            new StatModifierAffix(StatType.Attack, ModifierValue.Add(10)),
            new StatModifierAffix(StatType.Defence, ModifierValue.Add(10)),
            new SkillBonusModifierAffix("墨拳", ModifierValue.Add(0.3)),
        ],
    };

    [Fact]
    public void FormatsTableAttackDefenceAsPercentWithoutRescaling()
    {
        var text = TitleDescriptionFormatter.FormatBbCodeCn(
            MojiaJuzi(), TestContentFactory.CreateRepository());

        Assert.Contains("+攻击 10%", text, StringComparison.Ordinal);
        Assert.Contains("+防御 10%", text, StringComparison.Ordinal);
        Assert.Contains("+奥义威力 10", text, StringComparison.Ordinal);
        Assert.Contains("+奥义发动概率 10%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1000%", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SkipsHeaderDuplicatingAttackDefencePassiveLines()
    {
        var text = TitleDescriptionFormatter.FormatBbCodeCn(
            MojiaJuzi(), TestContentFactory.CreateRepository());

        Assert.Contains("被动增益：", text, StringComparison.Ordinal);
        Assert.DoesNotContain("攻击力", text, StringComparison.Ordinal);
        Assert.DoesNotContain("防御力", text, StringComparison.Ordinal);
        Assert.Contains("(√)", text, StringComparison.Ordinal);
        Assert.Contains("墨拳", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OmitsPassiveSectionWithoutAffixes()
    {
        var title = MojiaJuzi() with { Affixes = [] };
        var text = TitleDescriptionFormatter.FormatBbCodeCn(
            title, TestContentFactory.CreateRepository());

        Assert.DoesNotContain("被动增益", text, StringComparison.Ordinal);
        Assert.Contains("+攻击 10%", text, StringComparison.Ordinal);
    }
}
