using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleLogDrawer : Control
{
	private const int MaximumLogCount = 12;
	private const double TransitionDurationSeconds = 0.2d;
	private const float ClosedTagOvershoot = 20f;

	private readonly List<string> _logLines = [];

	private Control _drawerBody = null!;
	private Control _background = null!;
	private RichTextLabel _logLabel = null!;
	private BaseButton _tagButton = null!;
	private Tween? _transition;
	private Vector2 _openBodyPosition;
	private Vector2 _closedBodyPosition;
	private Vector2 _openTagPosition;
	private Vector2 _closedTagPosition;
	private bool _isOpen;

	public override void _Ready()
	{
		_drawerBody = GetNode<Control>("%DrawerBody");
		_background = GetNode<Control>("%Background");
		_logLabel = GetNode<RichTextLabel>("%LogLabel");
		_tagButton = GetNode<BaseButton>("%BattleLogTag");

		_openBodyPosition = _drawerBody.Position;
		_openTagPosition = _tagButton.Position;
		var bodyRightEdge = MathF.Max(
			_drawerBody.GlobalPosition.X + _drawerBody.Size.X,
			_background.GlobalPosition.X + _background.Size.X);
		_closedBodyPosition = _openBodyPosition - new Vector2(bodyRightEdge, 0f);
		_closedTagPosition = _openTagPosition - new Vector2(
			_tagButton.GlobalPosition.X + ClosedTagOvershoot,
			0f);
		_tagButton.Pressed += Toggle;

		ApplyClosedState();
		RefreshText();
	}

	public override void _ExitTree()
	{
		_transition?.Kill();
		_transition = null;
		base._ExitTree();
	}

	public void Append(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		_logLines.Add(text);
		if (_logLines.Count > MaximumLogCount)
		{
			_logLines.RemoveAt(0);
		}

		if (IsNodeReady())
		{
			RefreshText();
		}
	}

	public void Clear()
	{
		_logLines.Clear();
		if (IsNodeReady())
		{
			RefreshText();
		}
	}

	private void Toggle()
	{
		_isOpen = !_isOpen;
		_tagButton.Disabled = true;
		_transition?.Kill();

		_transition = CreateTween();
		_transition
			.TweenProperty(
				_drawerBody,
				"position",
				_isOpen ? _openBodyPosition : _closedBodyPosition,
				TransitionDurationSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_transition
			.Parallel()
			.TweenProperty(
				_tagButton,
				"position",
				_isOpen ? _openTagPosition : _closedTagPosition,
				TransitionDurationSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_transition.TweenCallback(Callable.From(CompleteTransition));
	}

	private void CompleteTransition()
	{
		_transition = null;
		_tagButton.Disabled = false;
	}

	private void ApplyClosedState()
	{
		_isOpen = false;
		_drawerBody.Position = _closedBodyPosition;
		_tagButton.Position = _closedTagPosition;
		_tagButton.Disabled = false;
	}

	private void RefreshText() => _logLabel.Text = string.Join('\n', _logLines);
}
