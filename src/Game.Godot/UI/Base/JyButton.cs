using Godot;

namespace Game.Godot.UI;

[GlobalClass]
public partial class JyButton : TextureButton
{
	[Export]
	public Vector2 PressOffset { get; set; } = new(2, 2);

	[Export]
	public Color PressColor { get; set; } = new(0.8f, 0.8f, 0.8f);

	[Export]
	public string PressSfxId { get; set; } = "音效.UI.点击";

	private Vector2 _originPosition;
	private bool _signalsConnected;

	public override void _EnterTree()
	{
		if (_signalsConnected)
		{
			return;
		}

		ButtonDown += OnButtonDown;
		ButtonUp += OnButtonUp;
		_signalsConnected = true;
	}

	private void OnButtonDown()
	{
		_originPosition = Position;
		Position = _originPosition + PressOffset;
		Modulate = PressColor;
		global::Game.Godot.Game.Audio.PlaySfx(PressSfxId);
		OnPressedDown();
	}

	private void OnButtonUp()
	{
		Position = _originPosition;
		Modulate = Colors.White;
		OnPressedUp();
	}

	protected virtual void OnPressedDown()
	{
	}

	protected virtual void OnPressedUp()
	{
	}
}
