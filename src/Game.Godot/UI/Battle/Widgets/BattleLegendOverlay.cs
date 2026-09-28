using Game.Core.Battle;
using Game.Godot.Resources;
using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleLegendOverlay : Control
{
	private static readonly Vector2 DesignSize = new(1920f, 1080f);
	private static readonly StringName LegendIntroAnimationName = new("legend_intro");

	private Control _effectRoot = null!;
	private Control _designRoot = null!;
	private TextureRect _portrait = null!;
	private Label _skillNameLabel = null!;
	private BattleSkillView _effectView = null!;
	private AnimationPlayer _legendAnimationPlayer = null!;

	public override void _Ready()
	{
		_effectRoot = GetNode<Control>("%EffectRoot");
		_designRoot = GetNode<Control>("%DesignRoot");
		_portrait = GetNode<TextureRect>("%Portrait");
		_skillNameLabel = GetNode<Label>("%SkillNameLabel");
		_effectView = GetNode<BattleSkillView>("%EffectView");
		_legendAnimationPlayer = GetNode<AnimationPlayer>("%LegendAnimationPlayer");
		ApplyResponsiveScale();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationResized && _designRoot is not null && _effectRoot is not null)
		{
			ApplyResponsiveScale();
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

	private void ApplyResponsiveScale()
	{
		var designScale = Math.Min(Size.X / DesignSize.X, Size.Y / DesignSize.Y);
		if (designScale <= 0f)
		{
			designScale = 1f;
		}

		_designRoot.PivotOffset = DesignSize * 0.5f;
		_designRoot.Scale = new Vector2(designScale, designScale);

		var effectScale = new Vector2(Size.X / DesignSize.X, Size.Y / DesignSize.Y);
		if (effectScale.X <= 0f || effectScale.Y <= 0f)
		{
			effectScale = Vector2.One;
		}

		_effectRoot.PivotOffset = DesignSize * 0.5f;
		_effectRoot.Scale = effectScale;
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
