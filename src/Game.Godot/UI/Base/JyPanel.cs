using Godot;

namespace Game.Godot.UI;

public partial class JyPanel : Control
{
	[Signal]
	public delegate void ClosePanelRequestedEventHandler();

	protected JyButton CloseButton { get; private set; } = null!;

	public override void _Ready()
	{
		CloseButton = GetNode<JyButton>("%CloseButton");
		ClosePanelRequested += QueueFree;
	}

	private void OnCloseButtonPressed()
	{
		EmitSignal(SignalName.ClosePanelRequested);
	}
}
