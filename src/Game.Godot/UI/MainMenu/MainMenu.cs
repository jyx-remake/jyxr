using Game.Application;
using Game.Core.Definitions;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class MainMenu : Control
{
	private TextureButton _startButton = null!;
	private TextureButton _loadButton = null!;
	private TextureButton _musicButton = null!;
	private TextureRect _background = null!;
	private bool _isStarting;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Bg");
		_startButton = GetNode<TextureButton>("%StartButton");
		_loadButton = GetNode<TextureButton>("%LoadButton");
		_musicButton = GetNode<TextureButton>("%MusicButton");

		_startButton.Pressed += OnStartPressed;
		_loadButton.Pressed += OnLoadPressed;
		_musicButton.Pressed += OnMusicPressed;

		PreviewGameBootstrap.Initialize();
		_background.Texture = AssetResolver.LoadTextureResource(Game.Config.MainMenuBackground);
		UIRoot.Instance.SetHudSuppressed(true);
		Game.Audio.PlayBgm(Game.Config.MainMenuMusic);
	}

	private async void OnStartPressed()
	{
		if (_isStarting)
		{
			return;
		}

		_isStarting = true;
		SetButtonsDisabled(true);

		try
		{
			Hide();
			await GameFlow.StartNewGameAsync();
		}
		catch (Exception exception)
		{
			Show();
			Game.Logger.Error("Starting new game failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
		finally
		{
			_isStarting = false;
			SetButtonsDisabled(false);
		}
	}

	private async void OnLoadPressed()
	{
		var panel = UIRoot.Instance.ShowSaveSlotSelectionPanel(SaveSlotPanelMode.Load);
		await ToSignal(panel, Node.SignalName.TreeExited);

		if (string.IsNullOrWhiteSpace(Game.State.Location.CurrentMapId))
		{
			return;
		}

		Hide();
		UIRoot.Instance.SetHudSuppressed(false);
		UIRoot.Instance.ShowHud();
	}

	private async void OnMusicPressed()
	{
		var tracks = BuildMusicChoices();
		var options = tracks
			.Select(FormatMusicChoice)
			.Append("返回")
			.ToArray();
		var selectedIndex = await UIRoot.Instance.ShowChoicesAsync(null, "音乐欣赏", options);
		if (selectedIndex < 0 || selectedIndex >= tracks.Count)
		{
			return;
		}

		Game.Audio.PlayBgm(tracks[selectedIndex].Id);
	}

	private IReadOnlyList<ResourceDefinition> BuildMusicChoices()
	{
		return Game.ContentRepository
			.GetResourcesByGroup("音乐")
			.Concat(Game.ContentRepository.GetResourcesByGroup("战斗音乐"))
			.ToList();
	}

	private static string FormatMusicChoice(ResourceDefinition resource) => resource.Id;

	private void SetButtonsDisabled(bool disabled)
	{
		_startButton.Disabled = disabled;
		_loadButton.Disabled = disabled;
		_musicButton.Disabled = disabled;
	}
}
