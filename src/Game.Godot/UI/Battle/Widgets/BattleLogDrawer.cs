using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleLogDrawer : Control
{
	private const int MaximumLogCount = 200;
	private const double WheelScrollStep = 60d;
	private const double TransitionDurationSeconds = 0.2d;
	private const float ClosedTagOvershoot = 20f;

	private readonly List<string> _logLines = [];

	private Control _drawerBody = null!;
	private Control _background = null!;
	private RichTextLabel _logLabel = null!;
	private VScrollBar _scrollBar = null!;
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
		_scrollBar = _logLabel.GetVScrollBar();
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
		RefreshText(followLatest: true, scrollOffset: 0d);
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

		var isReady = IsNodeReady();
		var scrollOffset = 0d;
		var followLatest = true;
		if (isReady)
		{
			_logLabel.GetLineCount();
			scrollOffset = _scrollBar.Value;
			followLatest = scrollOffset >= _scrollBar.MaxValue - _scrollBar.Page - 1d;
		}

		_logLines.Add(text.ReplaceLineEndings("\n"));
		if (_logLines.Count > MaximumLogCount)
		{
			if (isReady && !followLatest)
			{
				var removedParagraphs = _logLines[0].Count(character => character == '\n') + 1;
				scrollOffset -= _logLabel.GetParagraphOffset(removedParagraphs) - _logLabel.GetParagraphOffset(0);
			}
			_logLines.RemoveAt(0);
		}

		if (isReady)
		{
			RefreshText(followLatest, scrollOffset);
		}
	}

	public void Clear()
	{
		_logLines.Clear();
		if (IsNodeReady())
		{
			RefreshText(followLatest: true, scrollOffset: 0d);
		}
	}

	public bool TryScroll(InputEventMouseButton button)
	{
		if (!_isOpen || _transition is not null || !IsVisibleInTree())
		{
			return false;
		}

		var localPosition = _logLabel.GetGlobalTransformWithCanvas().AffineInverse() * button.Position;
		if (!new Rect2(Vector2.Zero, _logLabel.Size).HasPoint(localPosition))
		{
			return false;
		}

		var direction = button.ButtonIndex switch
		{
			MouseButton.WheelUp => -1,
			MouseButton.WheelDown => 1,
			_ => 0,
		};
		if (!button.Pressed || direction == 0)
		{
			return false;
		}

		_scrollBar.Value += direction * WheelScrollStep * button.Factor;
		return true;
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

	private void RefreshText(bool followLatest, double scrollOffset)
	{
		_logLabel.ScrollFollowing = followLatest;
		_logLabel.Text = string.Join('\n', _logLines);
		// Resolve wrapping and scrollbar bounds before restoring the reading position.
		_logLabel.GetLineCount();
		_scrollBar.Value = followLatest ? _scrollBar.MaxValue : Math.Max(0d, scrollOffset);
	}
}
