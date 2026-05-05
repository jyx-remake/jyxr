using Game.Application;
using Godot;

namespace Game.Godot.UI;

public partial class GameFinScreen : Control
{
	private TextureButton _mainMenuButton = null!;

	public override void _Ready()
	{
		_mainMenuButton = GetNode<TextureButton>("%MainMenuButton");
		_mainMenuButton.Pressed += OnMainMenuPressed;
		UIRoot.Instance.SetHudSuppressed(true);
	}

	private void OnMainMenuPressed()
	{
		GameFlow.ReturnToMainMenu();
	}
}
