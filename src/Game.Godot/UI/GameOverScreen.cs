using System.Globalization;
using Game.Application;
using Godot;

namespace Game.Godot.UI;

public partial class GameOverScreen : Control
{
	private Label _dateLabel = null!;
	private Label _deathInfoLabel = null!;
	private TextureButton _restartButton = null!;
	private TextureButton _loadGameButton = null!;
	private TextureButton _exitButton = null!;
	private IDisposable? _saveLoadedSubscription;
	private bool _isStarting;

	public override void _Ready()
	{
		_dateLabel = GetNode<Label>("%DateLabel");
		_deathInfoLabel = GetNode<Label>("%DeathInfoLabel");
		_restartButton = GetNode<TextureButton>("%RestartButton");
		_loadGameButton = GetNode<TextureButton>("%LoadGameButton");
		_exitButton = GetNode<TextureButton>("%ExitButton");

		_restartButton.Pressed += OnRestartPressed;
		_loadGameButton.Pressed += OnLoadGamePressed;
		_exitButton.Pressed += OnExitPressed;
		_saveLoadedSubscription = Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded);

		UIRoot.Instance.SetHudSuppressed(true);
		Refresh();
	}

	public override void _ExitTree()
	{
		_saveLoadedSubscription?.Dispose();
		_saveLoadedSubscription = null;
	}

	private void Refresh()
	{
		_dateLabel.Text = DateTime.Now.ToString("yyyy/M/d", CultureInfo.InvariantCulture);
		_deathInfoLabel.Text = $"您在这个世界已经累计死亡了{Game.Profile.DeathCount}次";
	}

	private async void OnRestartPressed()
	{
		if (_isStarting)
		{
			return;
		}

		_isStarting = true;
		SetButtonsDisabled(true);
		
		await GameFlow.StartNewGameAsync();
	}

	private void OnLoadGamePressed()
	{
		UIRoot.Instance.ShowSaveSlotSelectionPanel(SaveSlotPanelMode.Load);
	}

	private void OnExitPressed() => GetTree().Quit();

	private void OnSaveLoaded(SaveLoadedEvent _)
	{
		UIRoot.Instance.SetHudSuppressed(false);
		UIRoot.Instance.ShowHud();
		QueueFree();
	}

	private void SetButtonsDisabled(bool disabled)
	{
		_restartButton.Disabled = disabled;
		_loadGameButton.Disabled = disabled;
		_exitButton.Disabled = disabled;
	}
}
