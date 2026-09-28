using Game.Application;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Persistence;

public partial class QuickSaveCoordinator : Node
{
	private static readonly StringName QuickSaveAction = new("quick_save");
	private static readonly StringName QuickLoadAction = new("quick_load");
	private readonly LocalSaveStore _saveStore = new();

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		var isQuickSave = @event.IsActionPressed(QuickSaveAction);
		var isQuickLoad = @event.IsActionPressed(QuickLoadAction);
		if (!isQuickSave && !isQuickLoad)
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		if (!CanUseQuickSaveOrLoad())
		{
			return;
		}

		if (isQuickSave)
		{
			Save();
			return;
		}

		LoadAsync();
	}

	private static bool CanUseQuickSaveOrLoad() =>
		Game.IsInitialized &&
		Game.IsDesktopPlatform &&
		!GameFlow.IsMainMenuActive &&
		!UIRoot.Instance.IsStoryPresentationActive &&
		!UIRoot.Instance.IsBattleActive;

	private void Save()
	{
		if (Game.State.Adventure.NoRegret)
		{
			UIRoot.Instance.ShowToast("无悔周目只允许自动存档", ToastTone.Error);
			return;
		}

		try
		{
			_saveStore.SaveCurrentSession(LocalSaveId.Quick);
			Game.Audio.PlaySfx("音效.装备");
			UIRoot.Instance.ShowToast("已写入快速存档");
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Quick save failed.", exception);
		}
	}

	private async void LoadAsync()
	{
		try
		{
			if (!_saveStore.TryLoad(LocalSaveId.Quick, out var envelope, out _) || envelope is null)
			{
				return;
			}

			if (!await SaveLoadWarningCoordinator.ConfirmAsync(envelope))
			{
				return;
			}

			GameFlow.LoadSave(envelope.SaveGame);
			Game.Audio.PlaySfx("音效.装备");
			UIRoot.Instance.ShowToast("已读取快速存档");
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Quick load failed.", exception);
		}
	}
}
