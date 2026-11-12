using Godot;

namespace Game.Godot.UI;

internal sealed class PanelBackInputRouter(Func<Control?> getTarget)
{
	private Control? _target;
	private bool _captured;

	public bool HandleInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey key && key.IsActionPressed("ui_cancel", allowEcho: true))
		{
			var backTarget = getTarget();
			if (!key.Echo && backTarget is IPanelBackHandler backHandler)
			{
				ConsumeRightClick();
				backHandler.HandleBack();
			}

			return backTarget is not null;
		}

		if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Right } button)
		{
			return false;
		}

		if (button.Pressed)
		{
			_target = getTarget();
			_captured = _target is not null;
			return false;
		}

		var target = _target;
		var captured = _captured;
		Reset();
		if (captured && target is IPanelBackHandler handler && GodotObject.IsInstanceValid(target) &&
			ReferenceEquals(target, getTarget()))
		{
			handler.HandleBack();
		}

		return captured;
	}

	// A control that owns the press also owns the release of this gesture.
	public void ConsumeRightClick() => _target = null;

	public void Reset()
	{
		_target = null;
		_captured = false;
	}
}
