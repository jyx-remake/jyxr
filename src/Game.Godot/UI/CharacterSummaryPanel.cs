using System.Text;
using Game.Application.Formatters;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterSummaryPanel : Control
{
	public string CharacterId { get; set; } = string.Empty;

	private TextureRect _avatar = null!;
	private Label _nameLabel = null!;
	private Label _levelLabel = null!;
	private Label _hpLabel = null!;
	private Label _mpLabel = null!;
	private Label _statsLabel = null!;
	private Label _equipmentLabel = null!;
	private SkillTab _skillTab = null!;
	private TextureButton _closeButton = null!;

	public override void _Ready()
	{
		_avatar = GetNode<TextureRect>("%Avatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_levelLabel = GetNode<Label>("%LevelLabel");
		_hpLabel = GetNode<Label>("%HpLabel");
		_mpLabel = GetNode<Label>("%MpLabel");
		_statsLabel = GetNode<Label>("%StatsLabel");
		_equipmentLabel = GetNode<Label>("%EquipmentLabel");
		_skillTab = GetNode<SkillTab>("%SkillTab");
		_closeButton = GetNode<TextureButton>("%CloseButton");

		_closeButton.Pressed += ClosePanel;
		Render();
	}

	private void Render()
	{
		if (string.IsNullOrWhiteSpace(CharacterId))
		{
			throw new InvalidOperationException("CharacterSummaryPanel.CharacterId is required.");
		}

		var character = Game.State.Party.GetMember(CharacterId);
		var portrait = AssetResolver.LoadCharacterPortrait(character);
		if (portrait is not null)
		{
			_avatar.Texture = portrait;
		}

		_nameLabel.Text = character.Name;
		_levelLabel.Text = $"等级 {character.Level}";
		_hpLabel.Text = $"HP {ToDisplayStat(character.GetStat(StatType.MaxHp))}";
		_mpLabel.Text = $"MP {ToDisplayStat(character.GetStat(StatType.MaxMp))}";
		_statsLabel.Text = BuildStatsText(character);
		_equipmentLabel.Text = BuildEquipmentText(character);
		_skillTab.Setup(character);
	}

	private void ClosePanel()
	{
		QueueFree();
	}

	private static string BuildStatsText(CharacterInstance character)
	{
		var combatStats = CharacterCombatStatFormatter.Calculate(character);
		var builder = new StringBuilder();
		builder.Append($"攻击 {combatStats.Attack}");
		builder.Append("  ");
		builder.Append($"防御 {combatStats.Defence}");
		builder.Append("  ");
		builder.Append($"轻功 {ToDisplayStat(character.GetStat(StatType.Speed))}");
		builder.Append("  ");
		builder.Append($"移动 {ToDisplayStat(character.GetStat(StatType.Movement))}");
		return builder.ToString();
	}

	private static string BuildEquipmentText(CharacterInstance character)
	{
		if (character.EquippedItems.Count == 0)
		{
			return "装备：无";
		}

		var names = character.EquippedItems.Values
			.OrderBy(static equipment => equipment.Definition.SlotType)
			.Select(static equipment => $"{equipment.Definition.SlotType}: {equipment.Definition.Name}");
		return $"装备：{string.Join(" / ", names)}";
	}

	private static int ToDisplayStat(double value) => Mathf.RoundToInt(value);
}
