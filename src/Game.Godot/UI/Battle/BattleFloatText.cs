using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleFloatText : Node2D
{
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
			(float)GD.RandRange(-50.0, 50.0),
			-(150f + (float)GD.RandRange(0.0, 50.0)));

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
		_label.AddThemeFontSizeOverride("font_size", 30);

		var tween = CreateTween();
		tween.TweenProperty(this, "position:y", Position.Y - 100f, 2d);
		tween.Finished += QueueFree;
	}
}
