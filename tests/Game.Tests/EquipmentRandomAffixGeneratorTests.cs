using System.Text.Json;
using Game.Application;
using Game.Core;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Serialization;
using Game.Expressions;

namespace Game.Tests;

public sealed class EquipmentRandomAffixGeneratorTests
{
    [Theory]
    [InlineData("level-4-5", 4, 1, 2, 4)]
    [InlineData("level-4-5", 5, 5, 11, 22)]
    [InlineData("level-6-7", 6, 1, 3, 7)]
    [InlineData("level-6-7", 7, 5, 15, 30)]
    public void BaseContent_WeaponBonusesScaleWithLevelAndRound(
        string tableId, int level, int round, int min, int max)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "engine-free-rpg.slnx")))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        var tables = JsonSerializer.Deserialize<EquipmentRandomAffixTableDefinition[]>(
            File.ReadAllText(Path.Combine(directory.FullName, "mods", "jyxr-base", "data", "random-affix-tables.json")),
            GameJson.Default)!;
        var table = Assert.Single(tables, value => value.Id == tableId);
        var options = table.Options.Where(option => option.Kind == EquipmentRandomAffixKind.WeaponBonus).ToArray();
        Assert.Equal(4, options.Select(option => option.WeaponType).Distinct().Count());
        foreach (var option in options)
        {
            var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
                [Table("weapon", "true", option)]);
            var equipment = TestContentFactory.CreateEquipment("weapon", level: level);
            foreach (var upper in new[] { false, true })
            {
                var roll = EquipmentRandomAffixGenerator.GenerateSingleRoll(
                    equipment, repository, round, new EndpointRandomService(upper));
                var affix = Assert.IsType<WeaponBonusModifierAffix>(Assert.Single(roll.Affixes));
                Assert.Equal(option.WeaponType, affix.WeaponType);
                Assert.Equal((upper ? max : min) / 100d, affix.Value.Delta);
            }
        }
    }

    [Fact]
    public void TryGenerateSingleRoll_SpeedExcludesOnlyExistingValuesAcrossPools()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon", level: 7);
        var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
        [Table("speed", "true",
            Option(EquipmentRandomAffixKind.Speed) with { Pool = ["0.10"] },
            Option(EquipmentRandomAffixKind.Speed) with { Pool = ["0.1", "0.15"] })]);
        IReadOnlyList<IReadOnlyList<AffixDefinition>> excluded =
            [[new StatModifierAffix(StatType.Speed, ModifierValue.Add(0.1))]];

        var roll = EquipmentRandomAffixGenerator.TryGenerateSingleRoll(
            equipment, repository, 1, new EndpointRandomService(false), excluded);

        Assert.NotNull(roll);
        Assert.Equal(0.15, Assert.IsType<StatModifierAffix>(Assert.Single(roll.Affixes)).Value.Delta);
        Assert.Null(EquipmentRandomAffixGenerator.TryGenerateSingleRoll(
            equipment, repository, 1, new MaxRandomService(), [.. excluded, roll.Affixes]));
    }

    [Fact]
    public void GenerateRolls_AllowsDistinctSpeedValuesButNotDuplicates()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon");
        var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
            [Table("speed", "true", Option(EquipmentRandomAffixKind.Speed) with { Pool = ["0.1", "0.15"] })]);
        var rolls = EquipmentRandomAffixGenerator.GenerateRolls(
            equipment, repository, 1, 3, new AlternatingPoolRandomService());

        Assert.Equal(new[] { 0.1, 0.15 }, rolls.Select(roll =>
            Assert.IsType<StatModifierAffix>(Assert.Single(roll.Affixes)).Value.Delta));
    }

    [Fact]
    public void GenerateRolls_EvaluatesTableAndIntegerRangeExpressions()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon", level: 4);
        var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
        [
            Table("low", "item_level < 4", Option(EquipmentRandomAffixKind.Accuracy, Range("1", "1"))),
            Table("current", "item_level == 4 && round == 2", Option(EquipmentRandomAffixKind.Accuracy, Range("round * 3", "item_level * round"))),
        ]);

        var roll = Assert.Single(EquipmentRandomAffixGenerator.GenerateRolls(
            equipment, repository, round: 2, rollCount: 1, new MaxRandomService()));

        var affix = Assert.IsType<StatModifierAffix>(Assert.Single(roll.Affixes));
        Assert.Equal(StatType.Accuracy, affix.Stat);
        Assert.Equal(0.08d, affix.Value.Delta);
    }

    [Fact]
    public void GenerateRolls_UsesConfiguredAttackAndWeaponRanges()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon", level: 4);
        var attackRepository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
        [
            Table("attack", "true", Option(
                EquipmentRandomAffixKind.AttackCombo,
                Range("item_level * (1 + round * 2)", "item_level * 2 * (1 + round * 2)"),
                Range("1", "item_level"))),
        ]);
        var attackRoll = Assert.Single(EquipmentRandomAffixGenerator.GenerateRolls(
            equipment, attackRepository, round: 2, rollCount: 1, new MaxRandomService()));
        Assert.Collection(
            attackRoll.Affixes,
            affix => Assert.Equal(40d, Assert.IsType<StatModifierAffix>(affix).Value.Delta),
            affix => Assert.Equal(0.04d, Assert.IsType<StatModifierAffix>(affix).Value.Delta));

        var weaponOption = Option(
            EquipmentRandomAffixKind.WeaponBonus,
            Range("3", "5")) with
        {
            WeaponType = WeaponType.Jianfa,
        };
        var weaponRepository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
            [Table("weapon", "true", weaponOption)]);
        var weaponRoll = Assert.Single(EquipmentRandomAffixGenerator.GenerateRolls(
            equipment, weaponRepository, round: 2, rollCount: 1, new MaxRandomService()));

        Assert.Equal(0.05d, Assert.IsType<WeaponBonusModifierAffix>(Assert.Single(weaponRoll.Affixes)).Value.Delta);
    }

    [Fact]
    public void GenerateSingleRoll_FiltersSkillCandidatesAndEvaluatesHardFormula()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon", level: 4);
        var option = Option(
            EquipmentRandomAffixKind.ExternalSkillBonus,
            Range(
                "floor(3 * (round + 3) / (skill_hard / 2 + 1))",
                "floor(15 * (round + 3) / (skill_hard / 2 + 1)) + (skill_hard < 6 ? round * 15 : 0)")) with
        {
            CandidateWhen = Expr("skill_hard >= item_level - 1 && skill_hard <= item_level + 4"),
        };
        var repository = TestContentFactory.CreateRepository(
            externalSkills:
            [
                TestContentFactory.CreateExternalSkill("too_easy", hard: 1),
                TestContentFactory.CreateExternalSkill("eligible", hard: 5),
                TestContentFactory.CreateExternalSkill("too_hard", hard: 9),
            ],
            equipmentRandomAffixTables: [Table("skills", "true", option)]);

        var roll = EquipmentRandomAffixGenerator.GenerateSingleRoll(
            equipment, repository, round: 2, new MaxRandomService());

        var affix = Assert.IsType<SkillBonusModifierAffix>(Assert.Single(roll.Affixes));
        Assert.Equal("eligible", affix.SkillId);
        Assert.Equal(0.51d, affix.Value.Delta);
    }

    [Fact]
    public void GenerateSingleRoll_RollsAndRoundsDecimalRange()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon", level: 4);
        var decimalRange = Range("0.5", "item_level") with
        {
            Mode = EquipmentRandomAffixRangeMode.Decimal,
            DecimalPlaces = 2,
        };
        var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
            [Table("crit", "true", Option(EquipmentRandomAffixKind.CritChance, decimalRange))]);

        var roll = EquipmentRandomAffixGenerator.GenerateSingleRoll(
            equipment, repository, round: 1, new MaxRandomService());

        var affix = Assert.IsType<StatModifierAffix>(Assert.Single(roll.Affixes));
        Assert.Equal(0.04d, affix.Value.Delta);
    }

    [Fact]
    public void WeightedRandomSelector_UsesOneTicketAcrossTotalWeight()
    {
        var entries = new[]
        {
            new EquipmentRandomAffixCountWeight(1, 432),
            new EquipmentRandomAffixCountWeight(2, 288),
            new EquipmentRandomAffixCountWeight(3, 180),
            new EquipmentRandomAffixCountWeight(4, 100),
        };

        Assert.Equal(1, WeightedRandomSelector.Select(entries, static entry => entry.Weight, new TicketRandomService(0)).Count);
        Assert.Equal(2, WeightedRandomSelector.Select(entries, static entry => entry.Weight, new TicketRandomService(432)).Count);
        Assert.Equal(3, WeightedRandomSelector.Select(entries, static entry => entry.Weight, new TicketRandomService(720)).Count);
        Assert.Equal(4, WeightedRandomSelector.Select(entries, static entry => entry.Weight, new TicketRandomService(900)).Count);
    }

    [Fact]
    public void TryGenerateSingleRoll_ExcludesExistingKindsBeforeRandomSelection()
    {
        var equipment = TestContentFactory.CreateEquipment("weapon");
        var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
        [Table("test", "true",
            Option(EquipmentRandomAffixKind.Accuracy, Range("1", "1")),
            Option(EquipmentRandomAffixKind.Speed) with { Pool = ["2"] })]);
        IReadOnlyList<IReadOnlyList<AffixDefinition>> excluded =
            [[new StatModifierAffix(StatType.Speed, ModifierValue.Add(2))]];

        var roll = EquipmentRandomAffixGenerator.TryGenerateSingleRoll(
            equipment, repository, 1, new MaxRandomService(), excluded);

        Assert.NotNull(roll);
        Assert.Equal(EquipmentRandomAffixKind.Accuracy, roll.Kind);
        Assert.Null(EquipmentRandomAffixGenerator.TryGenerateSingleRoll(
            equipment, repository, 1, new MaxRandomService(), [.. excluded, roll.Affixes]));
    }

    private static EquipmentRandomAffixTableDefinition Table(
        string id,
        string when,
        params EquipmentRandomAffixOptionDefinition[] options) => new()
    {
        Id = id,
        When = Expr(when),
        Options = options,
    };

    private static EquipmentRandomAffixOptionDefinition Option(
        EquipmentRandomAffixKind kind,
        params EquipmentRandomAffixRangeDefinition[] ranges) => new()
    {
        Kind = kind,
        Weight = 1,
        Ranges = ranges,
    };

    private static EquipmentRandomAffixRangeDefinition Range(string min, string max) => new()
    {
        Min = Expr(min),
        Max = Expr(max),
    };

    private static ParsedExpression Expr(string source) => new ExpressionParser().ParseExpression(source);

    private sealed class EndpointRandomService(bool upper) : IRandomService
    {
        public double NextDouble() => upper ? 0.999999d : 0d;
        public int Next(int minInclusive, int maxExclusive) => upper ? maxExclusive - 1 : minInclusive;
    }

    private sealed class AlternatingPoolRandomService : IRandomService
    {
        private int _poolIndex;
        public double NextDouble() => 0d;
        public int Next(int minInclusive, int maxExclusive) =>
            maxExclusive - minInclusive == 2 ? minInclusive + _poolIndex++ % 2 : minInclusive;
    }

    private sealed class MaxRandomService : IRandomService
    {
        public double NextDouble() => 0.999999d;
        public int Next(int minInclusive, int maxExclusive) => maxExclusive - 1;
    }

    private sealed class TicketRandomService(int ticket) : IRandomService
    {
        public double NextDouble() => 0d;
        public int Next(int minInclusive, int maxExclusive) => ticket;
    }
}
