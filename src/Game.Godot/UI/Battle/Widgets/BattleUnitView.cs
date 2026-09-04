using Game.Core.Battle;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleUnitView : Node2D
{
	private static readonly StringName LibraryKey = new(string.Empty);
	private static readonly StringName HideSystemShadowMetadata = new("hide_system_shadow");

	private static readonly Color PlayerBarColor = new(0.78f, 0.18f, 0.18f, 1f);
	private static readonly Color EnemyBarColor = new(0.6f, 0.12f, 0.12f, 1f);
	private static readonly Color ManaBarColor = new(0.2f, 0.48f, 0.9f, 1f);
	private static readonly Color GaugeBarColor = new(0.95f, 0.95f, 0.95f, 1f);
	private static readonly Color RagePipColor = new(1f, 0.78f, 0.1f, 1f);
	private static readonly Color EnemyNameColor = Colors.Red;

	private Sprite2D _sprite = null!;
	private Sprite2D _shadow = null!;
	private Sprite2D _activeArrow = null!;
	private Node2D _animationSlot = null!;
	private AnimationPlayer _animationPlayer = null!;
	private AnimationTree _animationTree = null!;
	private AnimationNodeStateMachinePlayback _stateMachine = null!;
	private AnimationLibrary? _animationLibrary;
	private Control _tooltipHitArea = null!;
	private Label _nameLabel = null!;
	private Label _titleLabel = null!;
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
	private Node2D? _auraRoot;
	private string? _auraAnimationId;

	public string UnitId { get; private set; } = string.Empty;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("%Sprite");
		_shadow = GetNode<Sprite2D>("%Shadow");
		_activeArrow = GetNode<Sprite2D>("%ActiveArrow");
		_animationSlot = GetNode<Node2D>("%AnimationSlot");
		_animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		_animationTree = GetNode<AnimationTree>("%AnimationTree");
		_stateMachine = _animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
		_tooltipHitArea = GetNode<Control>("%TooltipHitArea");
		_nameLabel = GetNode<Label>("%NameLabel");
		_titleLabel = GetNode<Label>("%TitleLabel");
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
		_shadow.Visible = !HidesSystemShadow(unit.AnimationLibrary);
		_portraitTexture = unit.PortraitTexture;

		_nameLabel.Text = unit.Name;
		_nameLabel.SelfModulate = unit.IsPlayerUnit ? Colors.White : EnemyNameColor;
		_titleLabel.Text = unit.EquippedTitleName ?? string.Empty;
		_titleLabel.Visible = !string.IsNullOrWhiteSpace(unit.EquippedTitleName);
		UpdateBar(_hpBar, unit.Hp, unit.MaxHp, unit.IsPlayerUnit ? PlayerBarColor : EnemyBarColor);
		UpdateBar(_mpBar, unit.Mp, unit.MaxMp, ManaBarColor);
		UpdateBar(_gaugeBar, unit.ActionGauge, 100, GaugeBarColor);
		UpdateRagePips(unit.Rage);
		UpdateBuffList(unit.Buffs);
		_tooltipHitArea.TooltipText = $"生命 {unit.Hp}/{unit.MaxHp}\n内力 {unit.Mp}/{unit.MaxMp}\n怒气 {unit.Rage}/{BattleUnit.MaxRage}";
		_activeArrow.Visible = unit.IsActing;
		Visible = unit.IsAlive;
		Modulate = Colors.White;
		SetRoleEffect(unit.RoleEffect);
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
		var playerHasRuntimeLibrary = _animationPlayer.HasAnimationLibrary(LibraryKey);
		if ((ReferenceEquals(_animationLibrary, animationLibrary) &&
			(animationLibrary is not null || !playerHasRuntimeLibrary)) ||
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
		else
		{
			// The scene carries a character only as an editor preview.  Never leave
			// that texture visible when the requested runtime model is unresolved.
			_sprite.Texture = null;
		}

		_animationTree.Active = animationLibrary is not null;
		PlayIdle();
	}

	private void SetFacing(BattleFacing facing)
	{
		// Animation libraries own Sprite.scale for their per-model scale. Facing
		// lives on the parent so repeated animation evaluation cannot overwrite it.
		_animationSlot.Scale = new Vector2(facing == BattleFacing.Right ? 1f : -1f, 1f);
	}

	/// <summary>
	/// Attaches the character's battle aura visual (legacy role_effect): a
	/// looping animation sprite parented to the unit. Missing animation
	/// assets resolve to no aura instead of an error.
	/// </summary>
	private void SetRoleEffect(BattleRoleEffectVisual? aura)
	{
		if (aura is null)
		{
			ClearRoleEffect();
			return;
		}

		if (string.Equals(_auraAnimationId, aura.AnimationId, StringComparison.Ordinal) &&
			_auraRoot is not null && GodotObject.IsInstanceValid(_auraRoot))
		{
			return;
		}

		ClearRoleEffect();
		var library = AssetResolver.LoadSkillAnimation(aura.AnimationId);
		var animationName = GetLoopableAnimationName(library);
		if (library is null || animationName.IsEmpty)
		{
			return;
		}

		var looped = (AnimationLibrary)library.Duplicate(true);
		var animation = looped.GetAnimation(animationName);
		if (animation is Animation loopAnimation)
		{
			loopAnimation.LoopMode = Animation.LoopModeEnum.Linear;
		}

		var alpha = aura.Transparency is < 0.001 or > 0.999 ? 0.999f : (float)aura.Transparency;
		// Parent under the animation slot (same space as the body sprite) so
		// the effect lands on the character instead of the view origin.
		_auraRoot = new Node2D { Name = "AuraRoot" };
		var sprite = new Sprite2D
		{
			Name = "Sprite",
			// Converted skill libraries bake Unity-pivot offsets for an
			// uncentered Sprite2D (same as the body sprite and
			// BattleSkillView). Centered rendering would shift the aura by
			// half the texture size.
			Centered = false,
			Modulate = new Color(1f, 1f, 1f, alpha),
			ZIndex = aura.Order >= 1 ? 1 : 0,
		};
		var player = new AnimationPlayer { Name = "AuraPlayer" };
		_auraRoot.AddChild(sprite);
		_auraRoot.AddChild(player);
		_animationSlot.AddChild(_auraRoot);
		player.AddAnimationLibrary(LibraryKey, looped);
		player.Play(animationName);
		_auraAnimationId = aura.AnimationId;
	}

	private void ClearRoleEffect()
	{
		_auraAnimationId = null;
		if (_auraRoot is not null)
		{
			if (GodotObject.IsInstanceValid(_auraRoot))
			{
				_auraRoot.QueueFree();
			}

			_auraRoot = null;
		}
	}

	private static StringName GetLoopableAnimationName(AnimationLibrary? library)
	{
		if (library is null)
		{
			return new StringName();
		}

		foreach (var animationName in library.GetAnimationList())
		{
			return animationName;
		}

		return new StringName();
	}

	private static bool HidesSystemShadow(AnimationLibrary? animationLibrary) =>
		animationLibrary is not null &&
		animationLibrary.HasMeta(HideSystemShadowMetadata) &&
		animationLibrary.GetMeta(HideSystemShadowMetadata).AsBool();

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
