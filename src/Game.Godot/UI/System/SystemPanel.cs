using Game.Application;
using Game.Godot.Persistence;
using Game.Godot.Resources;
using Godot;

namespace Game.Godot.UI;

public partial class SystemPanel : Control
{
	private BaseButton _backButton = null!;
	private Button _consoleButton = null!;
	private Button _mainMenuButton = null!;
	private Button _exitGameButton = null!;
	private Button _loadButton = null!;
	private Button _saveButton = null!;
	private Button _deleteSaveButton = null!;
	private IDisposable? _adventureStateSubscription;

	public override void _Ready()
	{
		GetNode<TextureRect>("%Background").Texture = AssetResolver.LoadTexture("地图.竹林");
		_backButton = GetNode<BaseButton>("%BackButton");
		_consoleButton = GetNode<Button>("%ConsoleButton");
		_mainMenuButton = GetNode<Button>("%MainMenuButton");
		_exitGameButton = GetNode<Button>("%ExitGameButton");
		_loadButton = GetNode<Button>("%LoadButton");
		_saveButton = GetNode<Button>("%SaveButton");
		_deleteSaveButton = GetNode<Button>("%DeleteSaveButton");

		_backButton.Pressed += () => UIRoot.Instance.CloseMainPanel();
		_consoleButton.Pressed += OnConsolePressed;
		_mainMenuButton.Pressed += GameFlow.ReturnToMainMenu;
		_exitGameButton.Pressed += OnExitGamePressed;
		_loadButton.Pressed += () => OpenSaveSlots(SaveSlotPanelMode.Load, "load");
		_saveButton.Pressed += OnSavePressed;
		_deleteSaveButton.Pressed += () => OpenSaveSlots(SaveSlotPanelMode.Delete, "delete");

		_consoleButton.Visible = Game.Config.ConsoleEnabled;
		_adventureStateSubscription = Game.Session.Events.Subscribe<AdventureStateChangedEvent>(_ => ApplyNoRegretRestrictions());
		ApplyNoRegretRestrictions();
	}

	private async void OnExitGamePressed()
	{
		if (!await UIRoot.Instance.ShowConfirmAsync(
			"确认退出游戏吗？未保存的进度将会丢失。"))
		{
			return;
		}

		GetTree().Quit();
	}

	public override void _ExitTree()
	{
		_adventureStateSubscription?.Dispose();
		_adventureStateSubscription = null;
		base._ExitTree();
	}

	private void OnConsolePressed()
	{
		if (!Game.Config.ConsoleEnabled)
		{
			return;
		}

		try
		{
			UIRoot.Instance.ShowConsolePanel();
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Opening console panel failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void OnSavePressed()
	{
		if (Game.State.Adventure.NoRegret)
		{
			UIRoot.Instance.ShowSuggestion("无悔周目只允许自动存档。");
			return;
		}

		OpenSaveSlots(SaveSlotPanelMode.Save, "save");
	}

	private static void OpenSaveSlots(SaveSlotPanelMode mode, string operationName)
	{
		try
		{
			UIRoot.Instance.ShowSaveSlotSelectionPanel(mode);
		}
		catch (Exception exception)
		{
			Game.Logger.Error($"Opening {operationName} slot panel failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void ApplyNoRegretRestrictions()
	{
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
		{
			return;
		}

		var noRegret = Game.State.Adventure.NoRegret;
		_saveButton.Disabled = noRegret;
		_saveButton.Modulate = noRegret ? new Color(0.55f, 0.55f, 0.55f, 0.72f) : Colors.White;
		_saveButton.TooltipText = noRegret ? "无悔周目只允许自动存档。" : string.Empty;
	}
}
