using Godot;

namespace Game.Godot.UI;

public partial class JyPanel : Control, IPanelBackHandler
{
	[Signal]
	public delegate void ClosePanelRequestedEventHandler();

	protected JyButton CloseButton { get; private set; } = null!;

	public override void _Ready()
	{
		CloseButton = GetNode<JyButton>("%CloseButton");
		ClosePanelRequested += QueueFree;
	}

	public virtual void HandleBack()
	{
		if (CloseButton.IsVisibleInTree() && !CloseButton.Disabled)
		{
			OnCloseButtonPressed();
		}
	}

	private void OnCloseButtonPressed()
	{
		EmitSignal(SignalName.ClosePanelRequested);
	}
}
