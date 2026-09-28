using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Presentation.Items;

namespace Game.Tests;

public sealed class ItemCatalogFilterTests
{
    [Fact]
    public void EquipmentTags_AreDeduplicatedOrderedAndExcludeOtherItemTypes()
    {
        var sword = Equipment("sword", ("weapon", 10), ("mod.sword", 5));
        var armor = Equipment("armor", ("armor", 20));
        var book = Equipment("book", ("book", 1)) with { Type = ItemType.SkillBook };
        var filter = new ItemCatalogFilter();

        filter.UpdateItems([sword, sword, armor, book], ItemType.Equipment);

        Assert.Equal(["mod.sword", "weapon", "armor"], filter.AvailableTags.Select(tag => tag.Id));
        Assert.True(filter.Matches(sword));
        Assert.False(filter.Matches(book));
    }

    [Fact]
    public void SelectTag_FiltersBothPlainAndAffixedEquipmentAndAllRestoresUntaggedItems()
    {
        var sword = Equipment("sword", ("weapon", 10));
        var untagged = Equipment("untagged");
        var inventory = new Inventory();
        inventory.AddItem(sword, 3);
        inventory.AddEquipmentInstance(new EquipmentInstance("sword_1", sword,
            [new StatModifierAffix(StatType.Speed, ModifierValue.Add(1))]));
        inventory.AddItem(untagged);
        var filter = new ItemCatalogFilter();
        filter.UpdateItems(inventory.Entries.Select(entry => entry.Definition), ItemType.Equipment);

        filter.SelectTag("weapon");
        Assert.Equal(2, inventory.Entries.Count(entry => filter.Matches(entry.Definition)));
        filter.SelectTag(null);
        Assert.All(inventory.Entries, entry => Assert.True(filter.Matches(entry.Definition)));
    }

    [Fact]
    public void Refresh_PreservesAvailableSelectionAndClearsRemovedTag()
    {
        var sword = Equipment("sword", ("weapon", 10));
        var armor = Equipment("armor", ("armor", 20));
        var filter = new ItemCatalogFilter();
        filter.UpdateItems([sword, armor], ItemType.Equipment);
        filter.SelectTag("weapon");

        filter.UpdateItems([sword], ItemType.Equipment);
        Assert.Equal("weapon", filter.SelectedTagId);
        filter.UpdateItems([armor], ItemType.Equipment);
        Assert.Null(filter.SelectedTagId);
        Assert.True(filter.Matches(armor));
        filter.UpdateItems([], ItemType.Equipment);
        Assert.Empty(filter.AvailableTags);
    }

    [Fact]
    public void ChangingCategory_ResetsTagEvenWhenNewCategoryUsesSameTag()
    {
        var sword = Equipment("sword", ("shared", 10));
        var book = Equipment("book", ("shared", 10)) with { Type = ItemType.SkillBook };
        var filter = new ItemCatalogFilter();
        filter.UpdateItems([sword, book], ItemType.Equipment);
        filter.SelectTag("shared");

        filter.UpdateItems([sword, book], ItemType.SkillBook);
        Assert.Null(filter.SelectedTagId);
        Assert.True(filter.Matches(book));
        Assert.False(filter.Matches(sword));
        filter.UpdateItems([sword, book], null);
        Assert.Empty(filter.AvailableTags);
        Assert.True(filter.Matches(sword));
        Assert.True(filter.Matches(book));
    }

    [Fact]
    public void UnavailableTag_CannotBecomeSelected()
    {
        var filter = new ItemCatalogFilter();
        filter.UpdateItems([Equipment("sword", ("weapon", 10))], ItemType.Equipment);
        Assert.Throws<ArgumentException>(() => filter.SelectTag("missing"));
        Assert.Null(filter.SelectedTagId);
    }

    private static EquipmentDefinition Equipment(string id, params (string Id, int Order)[] tags)
    {
        var repository = TestContentFactory.CreateRepository();
        foreach (var tag in tags)
        {
            repository.ItemTags.Add(tag.Id, new ItemTagDefinition { Id = tag.Id, Name = tag.Id, Order = tag.Order });
        }
        var equipment = TestContentFactory.CreateEquipment(id) with { TagIds = tags.Select(tag => tag.Id).ToArray() };
        equipment.ResolveTags(repository);
        return equipment;
    }
}
