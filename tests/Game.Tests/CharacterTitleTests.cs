using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Tests;

public sealed class CharacterTitleTests
{
    [Fact]
    public void EquipTitleProjectsOnlyTheSelectedTitleAffixes()
    {
        var definition = TestContentFactory.CreateCharacterDefinition("hero");
        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);
        var first = new CharacterTitleDefinition
        {
            Id = "first",
            Name = "First",
            Affixes = [new StatModifierAffix(StatType.Attack, ModifierValue.Add(5))],
        };
        var second = new CharacterTitleDefinition
        {
            Id = "second",
            Name = "Second",
            Affixes = [new StatModifierAffix(StatType.Attack, ModifierValue.Add(9))],
        };

        Assert.True(character.AddTitle(first, equipped: true));
        Assert.True(character.AddTitle(second));
        Assert.Equal(5, character.GetStat(StatType.Attack));
        Assert.True(character.EquipTitle("second"));
        Assert.Equal(9, character.GetStat(StatType.Attack));
        Assert.Single(character.Titles, title => title.Equipped && title.Id == "second");
    }

    [Fact]
    public void EquipTitleRejectsUnknownOwnedTitle()
    {
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));

        Assert.Throws<InvalidOperationException>(() => character.EquipTitle("missing"));
    }

    [Fact]
    public void SingleTitleCanBeCheckedAndUnchecked()
    {
        var character = TestContentFactory.CreateCharacterInstance(
            "char_001", TestContentFactory.CreateCharacterDefinition("hero"));
        var only = new CharacterTitleDefinition { Id = "only", Name = "Only" };

        Assert.True(character.AddTitle(only));
        Assert.False(character.Titles[0].Equipped);
        Assert.True(character.EquipTitle("only"));
        Assert.True(character.Titles[0].Equipped);
        Assert.True(character.EquipTitle(null));
        Assert.False(character.Titles[0].Equipped);
        Assert.True(character.EquipTitle("only"));
        Assert.True(character.Titles[0].Equipped);
    }

    [Fact]
    public void InitialCharacterTitlesAreResolvedAndEquipped()
    {
        var title = new CharacterTitleDefinition { Id = "initial", Name = "Initial" };
        var definition = new CharacterDefinition(
            "hero",
            "Hero",
            new Dictionary<StatType, int>(),
            [],
            [],
            [],
            [],
            InitialTitles: [new InitialCharacterTitleEntryDefinition("initial", Equipped: true)]);
        definition.Resolve(TestContentFactory.CreateRepository(characterTitles: [title]));

        var character = TestContentFactory.CreateCharacterInstance("char_001", definition);

        var equipped = Assert.Single(character.Titles);
        Assert.Equal("initial", equipped.Id);
        Assert.True(equipped.Equipped);
    }
}
