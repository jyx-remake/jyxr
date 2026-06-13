using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Godot.Assets;
using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.UI;

public partial class SaveSlotCard : Button
{
	private const float TapDragThreshold = 18f;

	public event Action? Tapped;

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
	private bool _pendingTap;
	private Vector2 _tapStartPosition;

	public int SlotIndex { get; private set; }

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("Margin/VBox/TitleLabel");
		_nameLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/NameLabel");
		_partyCountLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/PartyCountLabel");
		_gameTimeLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/GameTimeLabel");
		_difficultyLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/RunInfoRow/DifficultyLabel");
		_roundLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/RunInfoRow/RoundLabel");
		_locationLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/LocationLabel");
		_savedAtLabel = GetNode<Label>("Margin/VBox/ContentRow/InfoVBox/SavedAtLabel");
		_hintLabel = GetNode<Label>("Margin/VBox/HintLabel");
		_portrait = GetNode<TextureRect>("Margin/VBox/ContentRow/PortraitPanel/Portrait");
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (Disabled)
		{
			return;
		}

		switch (@event)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
				HandleMouseButton(mouseButton);
				break;
			case InputEventMouseMotion mouseMotion:
				CancelTapIfDragged(mouseMotion.Position);
				break;
		}
	}

	public void Configure(LocalSaveSlotSummary summary, SaveSlotPanelMode mode)
	{
		SlotIndex = summary.SlotIndex;
		Disabled = mode switch
		{
			SaveSlotPanelMode.Save => false,
			SaveSlotPanelMode.Load => !summary.CanLoad,
			SaveSlotPanelMode.Delete => !summary.HasSave,
			_ => throw new InvalidOperationException($"Unsupported save slot panel mode: {mode}"),
		};
		Modulate = Disabled
			? new Color(1f, 1f, 1f, 0.55f)
			: Colors.White;
		_titleLabel.Text = summary.Title ?? $"存档{summary.SlotIndex}";

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

		if (!summary.CanLoad)
		{
			_portrait.Texture = null;
			_nameLabel.Text = "不可读取";
			_partyCountLabel.Text = string.Empty;
			_gameTimeLabel.Text = string.Empty;
			_difficultyLabel.Text = string.Empty;
			_roundLabel.Text = string.Empty;
			_locationLabel.Text = string.Empty;
			_savedAtLabel.Text = string.Empty;
			_hintLabel.Modulate = new Color(0.98f, 0.72f, 0.24f);
			_hintLabel.Text = mode switch
			{
				SaveSlotPanelMode.Save => "该槽位存档不兼容，点击覆盖",
				SaveSlotPanelMode.Load => BuildInvalidSlotHint(summary.FailureReason),
				SaveSlotPanelMode.Delete => "该存档无法读取，点击删除",
				_ => throw new InvalidOperationException($"Unsupported save slot panel mode: {mode}"),
			};
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

	private static string BuildInvalidSlotHint(LocalSaveReadFailureReason failureReason) => failureReason switch
	{
		LocalSaveReadFailureReason.EnvelopeVersionMismatch or LocalSaveReadFailureReason.SaveVersionMismatch
			=> "该存档版本不兼容",
		LocalSaveReadFailureReason.InvalidFormat => "该存档已损坏或格式错误",
		LocalSaveReadFailureReason.MissingFile => "该槽位暂无存档",
		_ => "该存档无法读取",
	};

	private void HandleMouseButton(InputEventMouseButton mouseButton)
	{
		if (mouseButton.Pressed)
		{
			_pendingTap = true;
			_tapStartPosition = mouseButton.Position;
			return;
		}

		var shouldTap = _pendingTap &&
			_tapStartPosition.DistanceTo(mouseButton.Position) <= TapDragThreshold;
		_pendingTap = false;
		if (!shouldTap)
		{
			return;
		}

		Tapped?.Invoke();
		GetViewport().SetInputAsHandled();
	}

	private void CancelTapIfDragged(Vector2 localPosition)
	{
		if (_pendingTap && _tapStartPosition.DistanceTo(localPosition) > TapDragThreshold)
		{
			_pendingTap = false;
		}
	}
}
