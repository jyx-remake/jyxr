using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleFloatText : Node2D
{
	private const float RandomOffsetX = 50f;
	private const float FloatRiseDistance = 150f;
	private const float RandomRiseDistance = 50f;
	private const float PopupRiseDistance = 100f;
	private const int PopupFontSize = 30;

	private Label _label = null!;

	public override void _Ready()
	{
		_label = GetNode<Label>("%TextLabel");
	}

	public void Play(string text, Color color)
	{
		_label.Text = text;
		_label.Modulate = color;
		Scale = Vector2.One;

		var endPosition = Position + new Vector2(
			(float)GD.RandRange(-RandomOffsetX, RandomOffsetX),
			-(FloatRiseDistance + (float)GD.RandRange(0.0, RandomRiseDistance)));

		var tween = CreateTween();
		tween.TweenProperty(this, "position", endPosition, 1.5d)
			.SetTrans(Tween.TransitionType.Elastic)
			.SetEase(Tween.EaseType.Out);
		tween.Parallel()
			.TweenProperty(this, "scale", new Vector2(1.3f, 1.3f), 1.5d)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
		tween.Finished += QueueFree;
	}

	public void PlayPopup(string text, Color color)
	{
		_label.Text = text;
		_label.Modulate = color;
		Scale = Vector2.One;
		_label.AddThemeFontSizeOverride("font_size", PopupFontSize);

		var tween = CreateTween();
		tween.TweenProperty(this, "position:y", Position.Y - PopupRiseDistance, 2d);
		tween.Finished += QueueFree;
	}
}
