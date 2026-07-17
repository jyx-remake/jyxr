using Game.Application.Formatters;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Skills;
using Game.Godot.Assets;
using Game.Presentation.Items;
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
			ItemCatalogPresentation.FormatCategory(item),
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
			ItemCatalogPresentation.FormatCategory(item),
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
			ItemCatalogPresentation.FormatCategory(equipment.Definition),
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
