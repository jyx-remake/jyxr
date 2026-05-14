using Game.Application;
using Game.Godot.Persistence;
using Godot;

namespace Game.Godot.UI;

public enum SaveSlotPanelMode
{
	Save,
	Load,
	Delete,
}

public partial class SaveSlotSelectionPanel : JyPanel
{
	private readonly LocalSaveStore _saveStore = new();

	private SaveSlotPanelMode _mode;
	private PackedScene _slotCardScene = null!;
	private GridContainer _slotsGrid = null!;
	private Label _titleLabel = null!;
	private Label _subtitleLabel = null!;
	private CheckBox _skipConfirmationCheckBox = null!;
	private IReadOnlyList<SaveSlotCard> _slotCards = [];
	private SaveSlotCard? _autoSaveCard;

	public void Configure(SaveSlotPanelMode mode) => _mode = mode;

	public override void _Ready()
	{
		base._Ready();

		_titleLabel = GetNode<Label>("%TitleLabel");
		_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		_skipConfirmationCheckBox = GetNode<CheckBox>("%SkipConfirmationCheckBox");
		_slotsGrid = GetNode<GridContainer>("%SlotsGrid");
		_slotCardScene = GD.Load<PackedScene>("res://scenes/ui/system_panel/save_slot_card.tscn");
		_slotCards =
		[
			GetNode<SaveSlotCard>("%SlotCard1"),
			GetNode<SaveSlotCard>("%SlotCard2"),
			GetNode<SaveSlotCard>("%SlotCard3"),
			GetNode<SaveSlotCard>("%SlotCard4"),
		];

		for (var index = 0; index < _slotCards.Count; index++)
		{
			var slotIndex = index + 1;
			_slotCards[index].Pressed += () => OnSlotPressed(slotIndex);
		}
		CreateAutoSaveCardIfNeeded();

		ApplyModeText();
		RefreshSlots();
	}

	private void ApplyModeText()
	{
		switch (_mode)
		{
			case SaveSlotPanelMode.Save:
				_titleLabel.Text = "选择存档位置";
				_subtitleLabel.Text = "选择存档保存游戏。";
				_skipConfirmationCheckBox.Show();
				return;
			case SaveSlotPanelMode.Load:
				_titleLabel.Text = "选择读档位置";
				_subtitleLabel.Text = "选择存档继续游戏。";
				_skipConfirmationCheckBox.Hide();
				return;
			case SaveSlotPanelMode.Delete:
				_titleLabel.Text = "选择删除存档";
				_subtitleLabel.Text = "选择一个槽位删除其存档数据。";
				_skipConfirmationCheckBox.Show();
				return;
			default:
				throw new InvalidOperationException($"Unsupported save slot panel mode: {_mode}");
		}
	}

	private void RefreshSlots()
	{
		if (_autoSaveCard is not null)
		{
			_autoSaveCard.Configure(_saveStore.GetAutoSaveSummary(), _mode);
		}

		foreach (var summary in _saveStore.GetSlotSummaries())
		{
			_slotCards[summary.SlotIndex - 1].Configure(summary, _mode);
		}
	}

	private void CreateAutoSaveCardIfNeeded()
	{
		if (_mode != SaveSlotPanelMode.Load)
		{
			return;
		}

		var instance = _slotCardScene.Instantiate();
		if (instance is not SaveSlotCard card)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Save slot card scene root must be SaveSlotCard.");
		}

		_autoSaveCard = card;
		_autoSaveCard.Pressed += () => LoadFromAutoSave();
		_slotsGrid.AddChild(card);
		_slotsGrid.MoveChild(card, 0);
	}

	private async void OnSlotPressed(int slotIndex)
	{
		try
		{
			if (_mode == SaveSlotPanelMode.Save)
			{
				await SaveToSlotAsync(slotIndex);
				return;
			}

			if (_mode == SaveSlotPanelMode.Delete)
			{
				await DeleteSlotAsync(slotIndex);
				return;
			}

			LoadFromSlot(slotIndex);
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Save slot action failed: mode={_mode}, slot={slotIndex}", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void LoadFromAutoSave()
	{
		if (!_saveStore.TryLoadAutoSave(out var envelope, out var failureReason) || envelope is null)
		{
			UIRoot.Instance.ShowSuggestion(BuildLoadFailureText(LocalSaveStore.AutoSaveSlotIndex, failureReason));
			RefreshSlots();
			return;
		}

		Game.LoadSave(envelope.SaveGame);
		ReloadCurrentMap();
		UIRoot.Instance.ShowToast("已读取自动存档");
		QueueFree();
	}

	private async Task SaveToSlotAsync(int slotIndex)
	{
		if (_saveStore.HasSave(slotIndex) && !ShouldSkipConfirmation())
		{
			var confirmed = await UIRoot.Instance.ShowConfirmAsync(BuildOverwriteConfirmationText(slotIndex));
			if (!confirmed)
			{
				return;
			}
		}

		_saveStore.SaveCurrentSession(slotIndex);
		UIRoot.Instance.ShowToast($"已写入存档{slotIndex}");
		QueueFree();
	}

	private async Task DeleteSlotAsync(int slotIndex)
	{
		if (!_saveStore.HasSave(slotIndex))
		{
			return;
		}

		if (!ShouldSkipConfirmation())
		{
			var confirmed = await UIRoot.Instance.ShowConfirmAsync(BuildDeleteConfirmationText(slotIndex));
			if (!confirmed)
			{
				return;
			}
		}

		if (!_saveStore.DeleteSave(slotIndex))
		{
			UIRoot.Instance.ShowSuggestion($"存档{slotIndex}不存在。");
			RefreshSlots();
			return;
		}

		UIRoot.Instance.ShowToast($"已删除存档{slotIndex}");
		RefreshSlots();
	}

	private void LoadFromSlot(int slotIndex)
	{
		if (!_saveStore.TryLoad(slotIndex, out var envelope, out var failureReason) || envelope is null)
		{
			UIRoot.Instance.ShowSuggestion(BuildLoadFailureText(slotIndex, failureReason));
			RefreshSlots();
			return;
		}

		Game.LoadSave(envelope.SaveGame);
		ReloadCurrentMap();
		UIRoot.Instance.ShowToast($"已读取存档{slotIndex}");
		QueueFree();
	}

	private static void ReloadCurrentMap()
	{
		var currentMapId = Game.State.Location.CurrentMapId;
		if (!string.IsNullOrWhiteSpace(currentMapId))
		{
			World.Instance.RefreshCurrentMap();
		}

		UIRoot.Instance.RefreshHud();
	}

	private bool ShouldSkipConfirmation() => _skipConfirmationCheckBox.ButtonPressed;

	private string BuildOverwriteConfirmationText(int slotIndex)
	{
		return $"存档{slotIndex}已有进度，确认覆盖吗？";
	}

	private string BuildDeleteConfirmationText(int slotIndex)
	{
		return $"确认删除存档{slotIndex}吗？";
	}

	private static string BuildLoadFailureText(int slotIndex, LocalSaveReadFailureReason failureReason) => failureReason switch
	{
		LocalSaveReadFailureReason.MissingFile when slotIndex == LocalSaveStore.AutoSaveSlotIndex => "自动存档不存在。",
		LocalSaveReadFailureReason.MissingFile => $"存档{slotIndex}不存在。",
		LocalSaveReadFailureReason.EnvelopeVersionMismatch or LocalSaveReadFailureReason.SaveVersionMismatch
			when slotIndex == LocalSaveStore.AutoSaveSlotIndex => "自动存档版本不兼容，无法读取。",
		LocalSaveReadFailureReason.EnvelopeVersionMismatch or LocalSaveReadFailureReason.SaveVersionMismatch
			=> $"存档{slotIndex}版本不兼容，无法读取。",
		LocalSaveReadFailureReason.InvalidFormat when slotIndex == LocalSaveStore.AutoSaveSlotIndex => "自动存档解析失败，无法读取。",
		LocalSaveReadFailureReason.InvalidFormat => $"存档{slotIndex}解析失败，无法读取。",
		_ when slotIndex == LocalSaveStore.AutoSaveSlotIndex => "自动存档无法读取。",
		_ => $"存档{slotIndex}无法读取。",
	};
}
