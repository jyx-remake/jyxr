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
		if (!CanUseQuickSave())
		{
			return;
		}

		if (isQuickSave)
		{
			Save();
			return;
		}

		Load();
	}

	private static bool CanUseQuickSave() =>
		Game.IsInitialized &&
		Game.IsDesktopPlatform &&
		!UIRoot.Instance.IsStoryPresentationActive &&
		!UIRoot.Instance.IsBattleActive;

	private void Save()
	{
		try
		{
			_saveStore.SaveCurrentSession(LocalSaveId.Quick);
			UIRoot.Instance.ShowToast("已写入快速存档");
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Quick save failed.", exception);
		}
	}

	private void Load()
	{
		try
		{
			if (!_saveStore.TryLoad(LocalSaveId.Quick, out var envelope, out _) || envelope is null)
			{
				return;
			}

			Game.LoadSave(envelope.SaveGame);
			UIRoot.Instance.ResetPresentationAfterLoad();
			UIRoot.Instance.ShowToast("已读取快速存档");
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Quick load failed.", exception);
		}
	}
}
