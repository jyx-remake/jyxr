using Godot;

namespace Game.Godot.UI.Battle;

public partial class BattleSkillView : Node2D
{
	private readonly StringName _libraryKey = new(string.Empty);
	private AnimationPlayer _animationPlayer = null!;

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
	}

	public void Play(AnimationLibrary? animationLibrary)
	{
		_ = PlayAsync(animationLibrary);
	}

	public async Task PlayAsync(AnimationLibrary? animationLibrary)
	{
		if (animationLibrary is null)
		{
			QueueFree();
			return;
		}

		if (_animationPlayer.HasAnimationLibrary(_libraryKey))
		{
			_animationPlayer.RemoveAnimationLibrary(_libraryKey);
		}

		_animationPlayer.AddAnimationLibrary(_libraryKey, animationLibrary);
		var defaultAnimation = new StringName("default");
		if (!_animationPlayer.HasAnimation(defaultAnimation))
		{
			QueueFree();
			return;
		}

		_animationPlayer.Play(defaultAnimation);
		await ToSignal(_animationPlayer, AnimationMixer.SignalName.AnimationFinished);
		if (GodotObject.IsInstanceValid(this))
		{
			QueueFree();
		}
	}
}
