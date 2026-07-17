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
	private IReadOnlyDictionary<LocalSaveId, SaveSlotCard> _saveCards = new Dictionary<LocalSaveId, SaveSlotCard>();

	public void Configure(SaveSlotPanelMode mode) => _mode = mode;

	public override void _Ready()
	{
		base._Ready();

		_titleLabel = GetNode<Label>("%TitleLabel");
		_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		_skipConfirmationCheckBox = GetNode<CheckBox>("%SkipConfirmationCheckBox");
		_slotsGrid = GetNode<GridContainer>("%SlotsGrid");
		_slotCardScene = GD.Load<PackedScene>("res://scenes/ui/system_panel/save_slot_card.tscn");
		_saveCards = CreateSaveCards();

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
				_subtitleLabel.Text = "选择存档删除其数据。";
				_skipConfirmationCheckBox.Show();
				return;
			default:
				throw new InvalidOperationException($"Unsupported save slot panel mode: {_mode}");
		}
	}

	private void RefreshSlots()
	{
		foreach (var (saveId, card) in _saveCards)
		{
			card.Configure(_saveStore.GetSummary(saveId), _mode);
		}
	}

	private IReadOnlyDictionary<LocalSaveId, SaveSlotCard> CreateSaveCards()
	{
		var cards = new Dictionary<LocalSaveId, SaveSlotCard>();
		foreach (var saveId in GetVisibleSaveIds())
		{
			var card = CreateSlotCard();
			card.Pressed += () => OnSaveCardPressed(saveId);
			_slotsGrid.AddChild(card);
			cards.Add(saveId, card);
		}

		return cards;
	}

	private IEnumerable<LocalSaveId> GetVisibleSaveIds()
	{
		if (_mode != SaveSlotPanelMode.Save)
		{
			yield return LocalSaveId.Auto;
			yield return LocalSaveId.Quick;
		}

		for (var slotIndex = 1; slotIndex <= LocalSaveStore.SlotCount; slotIndex++)
		{
			yield return LocalSaveId.Manual(slotIndex);
		}
	}

	private SaveSlotCard CreateSlotCard()
	{
		var instance = _slotCardScene.Instantiate();
		if (instance is SaveSlotCard card)
		{
			return card;
		}

		instance.QueueFree();
		throw new InvalidOperationException("Save slot card scene root must be SaveSlotCard.");
	}

	private async void OnSaveCardPressed(LocalSaveId saveId)
	{
		try
		{
			if (_mode == SaveSlotPanelMode.Save)
			{
				await SaveAsync(saveId);
				return;
			}

			if (_mode == SaveSlotPanelMode.Delete)
			{
				await DeleteAsync(saveId);
				return;
			}

			Load(saveId);
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Save action failed: mode={_mode}, save={saveId}", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private async Task SaveAsync(LocalSaveId saveId)
	{
		if (_saveStore.Exists(saveId) && !ShouldSkipConfirmation())
		{
			var confirmed = await UIRoot.Instance.ShowConfirmAsync($"{saveId.Title}已有进度，确认覆盖吗？");
			if (!confirmed)
			{
				return;
			}
		}

		_saveStore.SaveCurrentSession(saveId);
		UIRoot.Instance.ShowToast($"已写入{saveId.Title}");
		UIRoot.Instance.CloseMainPanel();
	}

	private async Task DeleteAsync(LocalSaveId saveId)
	{
		if (!_saveStore.Exists(saveId))
		{
			return;
		}

		if (!ShouldSkipConfirmation())
		{
			var confirmed = await UIRoot.Instance.ShowConfirmAsync($"确认删除{saveId.Title}吗？");
			if (!confirmed)
			{
				return;
			}
		}

		if (!_saveStore.Delete(saveId))
		{
			UIRoot.Instance.ShowSuggestion($"{saveId.Title}不存在。");
			RefreshSlots();
			return;
		}

		UIRoot.Instance.ShowToast($"已删除{saveId.Title}");
		RefreshSlots();
	}

	private void Load(LocalSaveId saveId)
	{
		if (!_saveStore.TryLoad(saveId, out var envelope, out var failureReason) || envelope is null)
		{
			UIRoot.Instance.ShowSuggestion(BuildLoadFailureText(saveId, failureReason));
			RefreshSlots();
			return;
		}

		Game.LoadSave(envelope.SaveGame);
		CompleteLoad($"已读取{saveId.Title}");
	}

	private void CompleteLoad(string toastText)
	{
		ReloadCurrentMap();
		UIRoot.Instance.ShowToast(toastText);
		UIRoot.Instance.CloseMainPanel();
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

	private static string BuildLoadFailureText(LocalSaveId saveId, LocalSaveReadFailureReason failureReason) => failureReason switch
	{
		LocalSaveReadFailureReason.MissingFile => $"{saveId.Title}不存在。",
		LocalSaveReadFailureReason.EnvelopeVersionMismatch or LocalSaveReadFailureReason.SaveVersionMismatch
			=> $"{saveId.Title}版本不兼容，无法读取。",
		LocalSaveReadFailureReason.InvalidFormat => $"{saveId.Title}解析失败，无法读取。",
		_ => $"{saveId.Title}无法读取。",
	};
}
