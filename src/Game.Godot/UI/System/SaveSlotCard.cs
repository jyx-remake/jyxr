using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Godot.Assets;
using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.UI;

public partial class SaveSlotCard : Button
{
	private Label _titleLabel = null!;
	private Label _nameLabel = null!;
	private Label _partyCountLabel = null!;
	private Label _gameTimeLabel = null!;
	private Label _difficultyLabel = null!;
	private Label _roundLabel = null!;
	private Label _locationLabel = null!;
	private Label _savedAtLabel = null!;
	private Label _hintLabel = null!;
	private TextureRect _portrait = null!;

	public int SlotIndex { get; private set; }

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_nameLabel = GetNode<Label>("%NameLabel");
		_partyCountLabel = GetNode<Label>("%PartyCountLabel");
		_gameTimeLabel = GetNode<Label>("%GameTimeLabel");
		_difficultyLabel = GetNode<Label>("%DifficultyLabel");
		_roundLabel = GetNode<Label>("%RoundLabel");
		_locationLabel = GetNode<Label>("%LocationLabel");
		_savedAtLabel = GetNode<Label>("%SavedAtLabel");
		_hintLabel = GetNode<Label>("%HintLabel");
		_portrait = GetNode<TextureRect>("%Portrait");
	}

	public void Configure(LocalSaveSlotSummary summary, SaveSlotPanelMode mode)
	{
		SlotIndex = summary.SlotIndex;
		Disabled = mode switch
		{
			SaveSlotPanelMode.Save => false,
			SaveSlotPanelMode.Load => !summary.HasSave,
			SaveSlotPanelMode.Delete => !summary.HasSave,
			_ => throw new InvalidOperationException($"Unsupported save slot panel mode: {mode}"),
		};
		Modulate = Disabled
			? new Color(1f, 1f, 1f, 0.55f)
			: Colors.White;
		_titleLabel.Text = $"存档{summary.SlotIndex}";

		if (!summary.HasSave)
		{
			_portrait.Texture = null;
			_nameLabel.Text = "空档";
			_partyCountLabel.Text = string.Empty;
			_gameTimeLabel.Text = string.Empty;
			_difficultyLabel.Text = string.Empty;
			_roundLabel.Text = string.Empty;
			_locationLabel.Text = string.Empty;
			_savedAtLabel.Text = string.Empty;
			_hintLabel.Modulate = Colors.White;
			_hintLabel.Text = mode == SaveSlotPanelMode.Save
				? "点击写入当前进度"
				: "该槽位暂无存档";
			return;
		}

		_portrait.Texture = AssetResolver.LoadCharacterPortrait(summary.LeaderPortrait);
		_nameLabel.Text = summary.LeaderName ?? "无名侠客";
		_partyCountLabel.Text = $"队伍人数：{summary.PartyMemberCount}";
		_gameTimeLabel.Text = BuildGameTimeText(summary.Clock);
		_difficultyLabel.Text = $"难度：{FormatDifficulty(summary.Difficulty)}";
		_roundLabel.Text = $"周目：{summary.Round}";
		_locationLabel.Text = $"当前位置：{ResolveMapName(summary.CurrentMapId)}";
		_savedAtLabel.Text = summary.SavedAtUtc is null
			? string.Empty
			: $"保存时间：{summary.SavedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}";
		_hintLabel.Text = mode switch
		{
			SaveSlotPanelMode.Save => "点击覆盖该存档",
			SaveSlotPanelMode.Load => "点击读取该存档",
			SaveSlotPanelMode.Delete => "点击删除该存档",
			_ => throw new InvalidOperationException($"Unsupported save slot panel mode: {mode}"),
		};
		_hintLabel.Modulate = mode == SaveSlotPanelMode.Delete
			? new Color(0.98f, 0.36f, 0.28f)
			: Colors.White;
	}

	private static string BuildGameTimeText(ClockRecord? clockRecord)
	{
		if (clockRecord is null)
		{
			return string.Empty;
		}

		return ClockFormatter.FormatDateTimeCn(ClockState.Restore(clockRecord));
	}

	private static string ResolveMapName(string? mapId)
	{
		if (string.IsNullOrWhiteSpace(mapId))
		{
			return "未进入地图";
		}

		if (Game.ContentRepository.TryGetMap(mapId, out var map))
		{
			return map.Name;
		}

		Game.Logger.Warning($"Save slot map definition is missing: {mapId}");
		return mapId;
	}

	private static string FormatDifficulty(GameDifficulty difficulty) => difficulty switch
	{
		GameDifficulty.Normal => "简单",
		GameDifficulty.Hard => "进阶",
		GameDifficulty.Crazy => "炼狱",
		_ => throw new InvalidOperationException($"Unsupported difficulty: {difficulty}"),
	};
}
