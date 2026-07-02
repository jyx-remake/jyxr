using Game.Application.Formatters;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public static class DetailPanelContentFactory
{
	public static DetailPanelContent CreateSkill(SkillInstance skill, DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(skill);

		return new DetailPanelContent(
			skill.Name,
			ResolveSkillCategory(skill),
			ResolveSkillIcon(skill),
			SkillDescriptionFormatter.FormatBbCodeCn(skill, Game.ContentRepository, Game.SkillMaxLevelPolicy),
			ResolveSkillTitleColor(skill),
			action);
	}

	public static DetailPanelContent CreateInventoryEntry(
		InventoryEntry entry,
		DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(entry);

		return entry switch
		{
			EquipmentInstanceInventoryEntry equipmentEntry => CreateEquipment(equipmentEntry.Equipment, action),
			_ => CreateItem(entry.Definition, action),
		};
	}

	public static DetailPanelContent CreateShopProduct(
		ShopProductView product,
		DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(product);

		var item = product.Item;
		return new DetailPanelContent(
			product.DisplayName,
			FormatItemType(item.Type),
			AssetResolver.LoadTextureResource(product.Picture),
			ItemDescriptionFormatter.FormatBbCodeCn(item, Game.ContentRepository),
			ResolveItemTitleColor(item),
			action);
	}

	public static DetailPanelContent CreateItem(
		ItemDefinition item,
		DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(item);

		return new DetailPanelContent(
			item.Name,
			FormatItemType(item.Type),
			AssetResolver.LoadTextureResource(item.Picture),
			ItemDescriptionFormatter.FormatBbCodeCn(item, Game.ContentRepository),
			ResolveItemTitleColor(item),
			action);
	}

	public static DetailPanelContent CreateEquipment(
		EquipmentInstance equipment,
		DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(equipment);

		return new DetailPanelContent(
			equipment.Definition.Name,
			FormatItemType(equipment.Definition.Type),
			AssetResolver.LoadTextureResource(equipment.Definition.Picture),
			ItemDescriptionFormatter.FormatBbCodeCn(equipment, Game.ContentRepository),
			ResolveEquipmentTitleColor(equipment),
			action);
	}

	private static string ResolveSkillCategory(SkillInstance skill) =>
		skill switch
		{
			InternalSkillInstance => "内功",
			ExternalSkillInstance => "外功",
			FormSkillInstance => "招式",
			LegendSkillInstance => "奥义",
			SpecialSkillInstance => "特技",
			_ => "技能",
		};

	private static Texture2D? ResolveSkillIcon(SkillInstance skill)
	{
		var icon = AssetResolver.LoadSkillIconResource(skill.Icon);
		if (icon is not null)
		{
			return icon;
		}

		return skill is FormSkillInstance formSkill
			? AssetResolver.LoadSkillIconResource(formSkill.Parent.Icon)
			: null;
	}

	private static Color ResolveSkillTitleColor(SkillInstance skill) =>
		skill switch
		{
			InternalSkillInstance => Colors.Magenta,
			SpecialSkillInstance => Colors.CornflowerBlue,
			FormSkillInstance => Colors.Red,
			LegendSkillInstance => Colors.Orange,
			_ => Colors.White,
		};

	private static string FormatItemType(ItemType itemType) =>
		itemType switch
		{
			ItemType.Equipment => "装备",
			ItemType.Consumable => "消耗品",
			ItemType.SkillBook => "武学书",
			ItemType.SpecialSkillBook => "绝技书",
			ItemType.TalentBook => "天赋书",
			ItemType.QuestItem => "剧情物品",
			ItemType.Booster => "强化道具",
			ItemType.Utility => "功能道具",
			_ => "物品",
		};

	private static Color ResolveItemTitleColor(ItemDefinition item) =>
		item.Type switch
		{
			ItemType.TalentBook => Colors.Magenta,
			ItemType.Booster => Colors.Red,
			ItemType.SkillBook or ItemType.SpecialSkillBook => Colors.Yellow,
			ItemType.Equipment => Colors.White,
			_ => Colors.White,
		};

	private static Color ResolveEquipmentTitleColor(EquipmentInstance equipment) =>
		EquipmentAffixGroupCounter.Count(equipment.ExtraAffixes) switch
		{
			>= 4 => Colors.Magenta,
			3 => Colors.Yellow,
			2 => Colors.Green,
			1 => Colors.CornflowerBlue,
			_ => Colors.White,
		};
}
