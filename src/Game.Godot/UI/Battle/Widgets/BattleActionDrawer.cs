using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleActionDrawer : Control
{
	private const double TransitionDurationSeconds = 0.2d;
	private const float CollapsedMargin = 10f;

	[Export]
	public NodePath ActionBarBackgroundPath { get; set; } = null!;

	private TextureRect _actionBarBackground = null!;
	private Control _actionButtons = null!;
	private TextureButton _avatar = null!;
	private Control _avatarFrame = null!;
	private Tween? _transition;
	private Vector2 _expandedAvatarPosition;
	private Vector2 _collapsedAvatarPosition;
	private Vector2 _expandedFramePosition;
	private Vector2 _collapsedFramePosition;
	private bool _isExpanded = true;
	public BaseButton MoveButton { get; private set; } = null!;
	public BaseButton StatusButton { get; private set; } = null!;
	public BaseButton ItemButton { get; private set; } = null!;
	public BaseButton RestButton { get; private set; } = null!;
	public BaseButton EndButton { get; private set; } = null!;

	public override void _Ready()
	{
		_actionBarBackground = GetNode<TextureRect>(ActionBarBackgroundPath);
		_actionButtons = GetNode<Control>("%ActionButtons");
		_avatar = GetNode<TextureButton>("%Avatar");
		_avatarFrame = GetNode<Control>("%AvatarFrame");
		MoveButton = GetNode<BaseButton>("%MoveButton");
		StatusButton = GetNode<BaseButton>("%StatusButton");
		ItemButton = GetNode<BaseButton>("%ItemButton");
		RestButton = GetNode<BaseButton>("%RestButton");
		EndButton = GetNode<BaseButton>("%EndButton");

		_expandedAvatarPosition = _avatar.Position;
		_expandedFramePosition = _avatarFrame.Position;
		var collapsedOffset = new Vector2(
			-CollapsedMargin - _avatarFrame.Position.X - _avatarFrame.Size.X,
			-CollapsedMargin - _avatarFrame.Position.Y - _avatarFrame.Size.Y);
		_collapsedAvatarPosition = _expandedAvatarPosition + collapsedOffset;
		_collapsedFramePosition = _expandedFramePosition + collapsedOffset;

		_avatar.Pressed += Toggle;
		ApplyExpandedState();
	}

	public override void _ExitTree()
	{
		_transition?.Kill();
		_transition = null;
		base._ExitTree();
	}

	public void SetAvatar(Texture2D? texture) => _avatar.TextureNormal = texture;

	private void Toggle()
	{
		_isExpanded = !_isExpanded;
		_avatar.Disabled = true;
		_transition?.Kill();

		if (_isExpanded)
		{
			_actionBarBackground.Show();
			_actionButtons.Show();
		}
		else
		{
			_actionBarBackground.Hide();
			_actionButtons.Hide();
		}

		_transition = CreateTween();
		_transition
			.TweenProperty(
				_avatar,
				"position",
				_isExpanded ? _expandedAvatarPosition : _collapsedAvatarPosition,
				TransitionDurationSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_transition
			.Parallel()
			.TweenProperty(
				_avatarFrame,
				"position",
				_isExpanded ? _expandedFramePosition : _collapsedFramePosition,
				TransitionDurationSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		_transition.TweenCallback(Callable.From(CompleteTransition));
	}

	private void CompleteTransition()
	{
		_transition = null;
		_avatar.TooltipText = _isExpanded ? "收起" : "展开";
		_avatar.Disabled = false;
	}

	private void ApplyExpandedState()
	{
		_isExpanded = true;
		_avatar.Position = _expandedAvatarPosition;
		_avatarFrame.Position = _expandedFramePosition;
		_actionBarBackground.Show();
		_actionButtons.Show();
		_avatar.TooltipText = "收起";
		_avatar.Disabled = false;
	}
}
