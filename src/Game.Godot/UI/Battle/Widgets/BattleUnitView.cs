using Game.Core.Battle;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleUnitView : Node2D
{
	private static readonly StringName LibraryKey = new(string.Empty);

	private static readonly Color PlayerBarColor = new(0.78f, 0.18f, 0.18f, 1f);
	private static readonly Color EnemyBarColor = new(0.6f, 0.12f, 0.12f, 1f);
	private static readonly Color ManaBarColor = new(0.2f, 0.48f, 0.9f, 1f);
	private static readonly Color GaugeBarColor = new(0.95f, 0.95f, 0.95f, 1f);
	private static readonly Color RagePipColor = new(1f, 0.78f, 0.1f, 1f);

	private Sprite2D _sprite = null!;
	private Sprite2D _activeArrow = null!;
	private AnimationPlayer _animationPlayer = null!;
	private AnimationTree _animationTree = null!;
	private AnimationNodeStateMachinePlayback _stateMachine = null!;
	private AnimationLibrary? _animationLibrary;
	private Control _tooltipHitArea = null!;
	private Label _nameLabel = null!;
	private RichTextLabel _buffListLabel = null!;
	private ProgressBar _hpBar = null!;
	private ProgressBar _mpBar = null!;
	private ProgressBar _gaugeBar = null!;
	private VBoxContainer _ragePips = null!;
	private Control _speechBubble = null!;
	private TextureRect _speechHead = null!;
	private Label _speechLabel = null!;
	private Texture2D? _portraitTexture;
	private ulong _speechSerial;

	public string UnitId { get; private set; } = string.Empty;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("%Sprite");
		_activeArrow = GetNode<Sprite2D>("%ActiveArrow");
		_animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		_animationTree = GetNode<AnimationTree>("%AnimationTree");
		_stateMachine = _animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
		_tooltipHitArea = GetNode<Control>("%TooltipHitArea");
		_nameLabel = GetNode<Label>("%NameLabel");
		_buffListLabel = GetNode<RichTextLabel>("%BuffListLabel");
		_hpBar = GetNode<ProgressBar>("%HpBar");
		_mpBar = GetNode<ProgressBar>("%MpBar");
		_gaugeBar = GetNode<ProgressBar>("%GaugeBar");
		_ragePips = GetNode<VBoxContainer>("%RagePips");
		_speechBubble = GetNode<Control>("%SpeechBubble");
		_speechHead = GetNode<TextureRect>("%SpeechHead");
		_speechLabel = GetNode<Label>("%SpeechLabel");
		_animationTree.Active = false;
	}

	public void Configure(BattleBoardUnitVisual unit)
	{
		ArgumentNullException.ThrowIfNull(unit);

		UnitId = unit.UnitId;
		SetAnimationSet(unit.AnimationLibrary);
		SetFacing(unit.Facing);
		_portraitTexture = unit.PortraitTexture;

		_nameLabel.Text = unit.Name;
		UpdateBar(_hpBar, unit.Hp, unit.MaxHp, unit.IsPlayerUnit ? PlayerBarColor : EnemyBarColor);
		UpdateBar(_mpBar, unit.Mp, unit.MaxMp, ManaBarColor);
		UpdateBar(_gaugeBar, unit.ActionGauge, 100, GaugeBarColor);
		UpdateRagePips(unit.Rage);
		UpdateBuffList(unit.Buffs);
		_tooltipHitArea.TooltipText = $"生命 {unit.Hp}/{unit.MaxHp}\n内力 {unit.Mp}/{unit.MaxMp}\n怒气 {unit.Rage}/{BattleUnit.MaxRage}";
		_activeArrow.Visible = unit.IsActing;
		Modulate = unit.IsAlive ? Colors.White : new Color(1f, 1f, 1f, 0.35f);
	}

	public async void ShowSpeech(string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		var serial = ++_speechSerial;
		_speechLabel.Text = text;
		_speechHead.Texture = _portraitTexture;
		_speechBubble.Visible = true;

		await ToSignal(GetTree().CreateTimer(2d), SceneTreeTimer.SignalName.Timeout);
		if (serial == _speechSerial && GodotObject.IsInstanceValid(this))
		{
			_speechBubble.Visible = false;
		}
	}

	public void PlayIdle()
	{
		TravelAnimation("idle");
	}

	public void PlayMoveLoop()
	{
		TravelAnimation("move");
	}

	public void PlayAttack(Action? completed = null)
	{
		TravelAnimation("attack");
		completed?.Invoke();
	}

	public void PlayHit(Action? completed = null)
	{
		TravelAnimation("hit");
		completed?.Invoke();
	}

	public void ApplyFacing(BattleFacing facing)
	{
		SetFacing(facing);
	}

	private void SetAnimationSet(AnimationLibrary? animationLibrary)
	{
		if (ReferenceEquals(_animationLibrary, animationLibrary) ||
			(_animationLibrary is not null &&
				animationLibrary is not null &&
				_animationLibrary.ResourcePath == animationLibrary.ResourcePath))
		{
			return;
		}

		if (_animationPlayer.HasAnimationLibrary(LibraryKey))
		{
			_animationPlayer.RemoveAnimationLibrary(LibraryKey);
		}

		_animationLibrary = animationLibrary;
		if (animationLibrary is not null)
		{
			_animationPlayer.AddAnimationLibrary(LibraryKey, animationLibrary);
		}

		_animationTree.Active = animationLibrary is not null;
		PlayIdle();
	}

	private void SetFacing(BattleFacing facing)
	{
		_sprite.Scale = new Vector2(facing == BattleFacing.Right ? 1f : -1f, 1f);
	}

	private void TravelAnimation(string animationName)
	{
		var stateName = new StringName(animationName);
		if (!_animationTree.Active || !_animationPlayer.HasAnimation(stateName))
		{
			return;
		}

		_stateMachine.Travel(stateName);
	}

	private static void UpdateBar(ProgressBar bar, int current, int max, Color color)
	{
		bar.MaxValue = Math.Max(max, 1);
		bar.Value = Mathf.Clamp(current, 0, Math.Max(max, 1));
		bar.Modulate = color;
	}

	private void UpdateRagePips(int rage)
	{
		var currentRage = Math.Clamp(rage, 0, BattleUnit.MaxRage);
		for (var index = 0; index < _ragePips.GetChildCount(); index++)
		{
			if (_ragePips.GetChild(index) is not ColorRect pip)
			{
				continue;
			}

			pip.Visible = index < currentRage;
			pip.Color = RagePipColor;
		}
	}

	private void UpdateBuffList(IReadOnlyList<BattleBoardBuffVisual> buffs)
	{
		if (buffs.Count == 0)
		{
			_buffListLabel.Text = string.Empty;
			_buffListLabel.Visible = false;
			return;
		}

		_buffListLabel.Text = string.Join('\n', buffs.Select(FormatBuffLine));
		_buffListLabel.Visible = true;
	}

	private static string FormatBuffLine(BattleBoardBuffVisual buff)
	{
		var levelText = buff.Level == 0 ? string.Empty : buff.Level.ToString();
		var turnsText = buff.RemainingTurns.ToString();
		var color = buff.IsDebuff ? "red" : "yellow";
		return $"[color={color}]{buff.Name}{levelText} {turnsText}[/color]";
	}
}
