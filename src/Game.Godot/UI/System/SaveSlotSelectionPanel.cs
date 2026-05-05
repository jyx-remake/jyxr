using Game.Application;
using Game.Core.Model;
using Game.Core.Persistence;
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
	private readonly LocalProfileStore _profileStore = new();

	private SaveSlotPanelMode _mode;
	private Label _titleLabel = null!;
	private Label _subtitleLabel = null!;
	private CheckBox _skipConfirmationCheckBox = null!;
	private IReadOnlyList<SaveSlotCard> _slotCards = [];

	public void Configure(SaveSlotPanelMode mode) => _mode = mode;

	public override void _Ready()
	{
		base._Ready();

		_titleLabel = GetNode<Label>("%TitleLabel");
		_subtitleLabel = GetNode<Label>("%SubtitleLabel");
		_skipConfirmationCheckBox = GetNode<CheckBox>("%SkipConfirmationCheckBox");
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
		foreach (var summary in _saveStore.GetSlotSummaries())
		{
			_slotCards[summary.SlotIndex - 1].Configure(summary, _mode);
		}
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
		_profileStore.SaveCurrentProfile();
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
		var envelope = _saveStore.Load(slotIndex);
		Game.LoadSave(envelope.SaveGame);
		LoadProfile(envelope);
		ReloadCurrentMap();
		UIRoot.Instance.ShowToast($"已读取存档{slotIndex}");
		QueueFree();
	}

	private void LoadProfile(LocalSaveEnvelope envelope)
	{
		if (_profileStore.HasProfile())
		{
			Game.ProfileService.LoadProfile(_profileStore.Load());
			return;
		}

		if (envelope.Profile is not null)
		{
			Game.ProfileService.LoadProfile(envelope.Profile);
			return;
		}

		Game.ProfileService.LoadProfile(GameProfileRecord.Create(new GameProfile()));
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
}
