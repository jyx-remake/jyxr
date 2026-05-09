using Game.Core.Battle;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleLegendOverlay : Control
{
	private static readonly Vector2 DesignSize = new(1920f, 1080f);
	private static readonly StringName LegendIntroAnimationName = new("legend_intro");

	private Control _designRoot = null!;
	private TextureRect _portrait = null!;
	private Label _skillNameLabel = null!;
	private BattleSkillView _effectView = null!;
	private AnimationPlayer _legendAnimationPlayer = null!;

	public override void _Ready()
	{
		_designRoot = GetNode<Control>("%DesignRoot");
		_portrait = GetNode<TextureRect>("%Portrait");
		_skillNameLabel = GetNode<Label>("%SkillNameLabel");
		_effectView = GetNode<BattleSkillView>("%EffectView");
		_legendAnimationPlayer = GetNode<AnimationPlayer>("%LegendAnimationPlayer");
		ApplyDesignScale();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized && _designRoot is not null)
		{
			ApplyDesignScale();
		}
	}

	public async Task PlayAsync(
		string casterName,
		Texture2D? portrait,
		BattleSkillCastInfo skillCast,
		Color accentColor)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(casterName);
		ArgumentNullException.ThrowIfNull(skillCast);

		_portrait.Texture = portrait;
		_skillNameLabel.Text = skillCast.ResolvedSkillName;

		var effectTask = PlayScreenEffectAsync(skillCast.ScreenEffectAnimationId);
		var sceneAnimationTask = PlaySceneAnimationAsync();

		await Task.WhenAll(sceneAnimationTask, effectTask);
		QueueFree();
	}

	private void ApplyDesignScale()
	{
		var scale = Math.Min(Size.X / DesignSize.X, Size.Y / DesignSize.Y);
		if (scale <= 0f)
		{
			scale = 1f;
		}

		_designRoot.PivotOffset = DesignSize * 0.5f;
		_designRoot.Scale = new Vector2(scale, scale);
	}

	private async Task PlaySceneAnimationAsync()
	{
		if (!_legendAnimationPlayer.HasAnimation(LegendIntroAnimationName))
		{
			return;
		}

		_legendAnimationPlayer.Play(LegendIntroAnimationName);
		await ToSignal(_legendAnimationPlayer, AnimationMixer.SignalName.AnimationFinished);
	}

	private async Task PlayScreenEffectAsync(string? animationId)
	{
		if (string.IsNullOrWhiteSpace(animationId))
		{
			return;
		}

		var animationLibrary = AssetResolver.LoadSkillAnimation(animationId);
		await _effectView.PlayAsync(animationLibrary);
	}
}
